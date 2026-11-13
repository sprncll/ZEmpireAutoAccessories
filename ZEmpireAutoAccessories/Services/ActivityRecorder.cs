using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Writes down who changed what, every time anything is saved.
    ///
    /// It sits in Entity Framework's save rather than in the screens: just
    /// before a save it notes each record being added, changed or deleted,
    /// and the fields that moved; just after, it writes those notes to
    /// sec.ActivityLog with who did it - in the same transaction, so a save
    /// that is rolled back takes its log with it.
    ///
    /// What is never written: password hashes, security stamps and tokens
    /// (a change to the password is logged as "changed", never its value),
    /// and the counters Identity bumps on every sign-in attempt, which would
    /// bury everything else.
    ///
    /// A log that cannot be written must never stop the work being logged:
    /// if the table is not there yet the recorder stands aside, and if a
    /// write fails it is reported to the site's log and the save it was
    /// describing still stands.
    /// </summary>
    public sealed class ActivityRecorder : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _http;
        private readonly SchemaStatus _schema;
        private readonly ILogger<ActivityRecorder> _log;

        // What each context is about to save, between "saving" and "saved".
        private readonly ConditionalWeakTable<DbContext, List<Pending>> _pending = new();

        // Contexts in the middle of writing their own log, so that save is
        // not itself logged.
        private readonly ConditionalWeakTable<DbContext, object> _writing = new();

        public ActivityRecorder(IHttpContextAccessor http, SchemaStatus schema, ILogger<ActivityRecorder> log)
        {
            _http = http;
            _schema = schema;
            _log = log;
        }

        // Identity's own bookkeeping: the rows themselves are not anything a
        // person would look for in a history.
        private static readonly HashSet<Type> Skipped = new()
        {
            typeof(ActivityLog),
            typeof(IdentityUserToken<string>),
            typeof(IdentityUserLogin<string>),
            typeof(IdentityUserClaim<string>),
            typeof(IdentityRoleClaim<string>),
        };

        // Moved by sign-in itself, every time; not something a person did.
        private static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
        {
            "AccessFailedCount", "LockoutEnd", "ConcurrencyStamp", "SecurityStamp",
            "NormalizedUserName", "NormalizedEmail", "NormalizedName",
        };

        // Recorded as changed, never with a value.
        private static bool IsSecret(string property) =>
            property.Contains("Password", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Token", StringComparison.OrdinalIgnoreCase)
            || property.Contains("Secret", StringComparison.OrdinalIgnoreCase);

        // The field that names a record, best first, for the summary line.
        private static readonly string[] NameFields =
        {
            "InvoiceNumber", "QuotationNumber", "JobOrderNumber", "PlateNumber",
            "FullName", "ProductName", "ServiceName", "UserName", "Name",
            "ClassificationName", "CategoryName", "JobTypeName", "PaymentModeName",
        };

        private sealed record Pending(EntityEntry Entry, string Action, string Entity, string Lines);

        // What a record can belong to, most specific first: a payment is part
        // of its invoice before it is anything of its customer's.
        private static readonly Type[] Documents =
        {
            typeof(ServiceInvoice), typeof(Sale), typeof(Quotation), typeof(JobOrder), typeof(Pricing),
            typeof(VehicleChecklist), typeof(InventoryCheck), typeof(Warranty),
            typeof(Product), typeof(Customer),
        };

        private static (string? Parent, string? Key) ParentOf(EntityEntry entry)
        {
            foreach (var document in Documents)
            {
                if (document == entry.Metadata.ClrType)
                    continue;

                var fk = entry.Metadata.GetForeignKeys()
                    .FirstOrDefault(f => f.PrincipalEntityType.ClrType == document && f.Properties.Count == 1);
                if (fk == null)
                    continue;

                var property = entry.Property(fk.Properties[0].Name);
                var value = entry.State == EntityState.Deleted ? property.OriginalValue : property.CurrentValue;
                if (value != null)
                    return (document.Name, Show(value));
            }

            return (null, null);
        }

        // ---- the save ----------------------------------------------------

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await Capture(eventData.Context);
            return result;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Capture(eventData.Context).GetAwaiter().GetResult();
            return result;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await Write(eventData.Context, cancellationToken);
            return result;
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            Write(eventData.Context, default).GetAwaiter().GetResult();
            return result;
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData) => Forget(eventData.Context);

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            Forget(eventData.Context);
            return Task.CompletedTask;
        }

        private void Forget(DbContext? context)
        {
            if (context != null)
                _pending.Remove(context);
        }

        private async Task Capture(DbContext? context)
        {
            if (context is not ApplicationDbContext db || _writing.TryGetValue(db, out _))
                return;

            if (!await _schema.Has(db, SchemaStatus.ActivityLog))
                return;

            var found = new List<Pending>();

            foreach (var entry in db.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                    continue;

                var type = entry.Metadata.ClrType;
                if (Skipped.Contains(type) || entry.Metadata.IsOwned())
                    continue;

                var lines = Describe(entry);

                // A change that only moved Identity's own counters - a failed
                // sign-in, a lockout ending - is not a change anyone made.
                if (entry.State == EntityState.Modified && lines.Length == 0)
                    continue;

                var action = entry.State switch
                {
                    EntityState.Added => ActivityLog.Added,
                    EntityState.Deleted => ActivityLog.Deleted,
                    _ => ActivityLog.Changed,
                };

                found.Add(new Pending(entry, action, type.Name, lines));
            }

            if (found.Count > 0)
                _pending.AddOrUpdate(db, found);
        }

        private async Task Write(DbContext? context, CancellationToken cancellationToken)
        {
            if (context is not ApplicationDbContext db || _writing.TryGetValue(db, out _))
                return;

            if (!_pending.TryGetValue(db, out var found))
                return;

            _pending.Remove(db);

            var (userId, userName) = WhoIsThis();
            var now = PhTime.Now;

            // Keys are read now, after the save: an added row has its id only
            // once the database has given it one.
            var rows = found.Select(p => new ActivityLog
            {
                Parent = ParentOf(p.Entry).Parent,
                ParentKey = ParentOf(p.Entry).Key,
                At = now,
                UserId = userId,
                UserName = userName,
                Action = p.Action,
                Entity = p.Entity,
                EntityKey = Truncate(KeyOf(p.Entry), 100),
                Summary = Truncate($"{p.Action} {Friendly(p.Entity)} {LabelOf(p.Entry)}".Trim(), 400),
                Changes = p.Lines.Length == 0 ? null : p.Lines,
            }).ToList();

            _writing.AddOrUpdate(db, new object());
            try
            {
                db.ActivityLogs.AddRange(rows);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "The activity log could not be written; the change itself was saved.");
            }
            finally
            {
                // Whatever happened, these rows are done with: left tracked,
                // a failed one would be retried by the next unrelated save.
                foreach (var row in rows)
                    db.Entry(row).State = EntityState.Detached;

                _writing.Remove(db);
            }
        }

        /// <summary>
        /// Something worth a line in the history that is not a database row:
        /// a receipt file attached, a sign-in. Never throws.
        /// </summary>
        public async Task Note(ApplicationDbContext db, string entity, string? key, string summary,
                               string? userId = null, string? userName = null)
        {
            try
            {
                if (!await _schema.Has(db, SchemaStatus.ActivityLog))
                    return;

                var who = WhoIsThis();
                var row = new ActivityLog
                {
                    At = PhTime.Now,
                    UserId = userId ?? who.UserId,
                    UserName = userName ?? who.UserName,
                    Action = ActivityLog.Note,
                    Entity = entity,
                    EntityKey = key,
                    Summary = Truncate(summary, 400),
                };

                _writing.AddOrUpdate(db, new object());
                try
                {
                    db.ActivityLogs.Add(row);
                    await db.SaveChangesAsync();
                }
                finally
                {
                    db.Entry(row).State = EntityState.Detached;
                    _writing.Remove(db);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "The activity log could not be written.");
            }
        }

        // ---- reading an entry ---------------------------------------------

        // Work the site does by itself - the expiry sweep - is the site's,
        // even when it runs inside somebody's request for the dashboard.
        private static readonly AsyncLocal<bool> _asSystem = new();

        /// <summary>Everything saved inside this scope is logged as "System".</summary>
        public static IDisposable AsSystem()
        {
            var before = _asSystem.Value;
            _asSystem.Value = true;
            return new Restore(() => _asSystem.Value = before);
        }

        private sealed class Restore : IDisposable
        {
            private readonly Action _undo;
            public Restore(Action undo) => _undo = undo;
            public void Dispose() => _undo();
        }

        private (string? UserId, string? UserName) WhoIsThis()
        {
            if (_asSystem.Value)
                return (null, "System");

            var user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return (null, "System");

            return (user.FindFirstValue(ClaimTypes.NameIdentifier), user.Identity.Name);
        }

        private static string Describe(EntityEntry entry)
        {
            var text = new StringBuilder();

            foreach (var property in entry.Properties)
            {
                var name = property.Metadata.Name;
                if (Noise.Contains(name) || property.Metadata.IsPrimaryKey() && entry.State != EntityState.Deleted)
                    continue;

                // Worked out by the database, not entered by anyone.
                if (property.Metadata.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate)
                    continue;

                switch (entry.State)
                {
                    case EntityState.Modified:
                        if (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue))
                            continue;
                        text.Append(name).Append(": ")
                            .Append(IsSecret(name) ? "changed" : Show(property.OriginalValue) + " → " + Show(property.CurrentValue))
                            .Append('\n');
                        break;

                    case EntityState.Added:
                        if (property.CurrentValue is null || IsSecret(name))
                            continue;
                        text.Append(name).Append(": ").Append(Show(property.CurrentValue)).Append('\n');
                        break;

                    case EntityState.Deleted:
                        if (property.OriginalValue is null || IsSecret(name))
                            continue;
                        text.Append(name).Append(": ").Append(Show(property.OriginalValue)).Append('\n');
                        break;
                }
            }

            return text.ToString().TrimEnd('\n');
        }

        private static string Show(object? value) => value switch
        {
            null => "(empty)",
            string s when s.Length == 0 => "(empty)",
            string s => Truncate(s, 200),
            decimal d => d.ToString("N2", CultureInfo.InvariantCulture),
            DateTime t => t.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            bool b => b ? "yes" : "no",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => Truncate(value.ToString() ?? "", 200),
        };

        private static string? KeyOf(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null)
                return null;

            return string.Join("/", key.Properties.Select(p => Show(entry.Property(p.Name).CurrentValue
                                                                    ?? entry.Property(p.Name).OriginalValue)));
        }

        private static string LabelOf(EntityEntry entry)
        {
            foreach (var field in NameFields)
            {
                var property = entry.Metadata.FindProperty(field);
                if (property == null)
                    continue;

                var value = entry.State == EntityState.Deleted
                    ? entry.Property(field).OriginalValue
                    : entry.Property(field).CurrentValue;

                if (value is string s && !string.IsNullOrWhiteSpace(s))
                    return s;
            }

            return KeyOf(entry) is { } id ? "#" + id : "";
        }

        // What a record is called in the shop, where the class name is not it.
        private static readonly Dictionary<string, string> PlainNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ServiceInvoice"] = "invoice",
            ["ServiceInvoiceDetail"] = "invoice item",
            ["ServiceInvoicePayment"] = "payment",
            ["Sale"] = "sale",
            ["SaleDetail"] = "sale item",
            ["JobOrderDetail"] = "job order item",
            ["QuotationDetail"] = "quotation item",
            ["InventoryTransaction"] = "stock movement",
            ["InventoryCheck"] = "stock count",
            ["InventoryCheckDetail"] = "stock count line",
            ["VehicleChecklistDetail"] = "checklist panel",
            ["Pricing"] = "price",
            ["ApplicationUser"] = "user account",
            ["ApplicationRole"] = "role",
            ["IdentityUserRole`1"] = "user's role",
            ["RolePermission"] = "role permission",
        };

        /// <summary>"ServiceInvoiceDetail" as "invoice item"; otherwise the words of the name.</summary>
        public static string Friendly(string entity)
        {
            if (PlainNames.TryGetValue(entity, out var plain))
                return plain;

            var words = new StringBuilder();
            for (var i = 0; i < entity.Length; i++)
            {
                if (i > 0 && char.IsUpper(entity[i]) && !char.IsUpper(entity[i - 1]))
                    words.Append(' ');
                words.Append(entity[i]);
            }

            var text = words.ToString()
                .Replace("Application ", "")
                .Replace("Identity ", "");
            return text.ToLowerInvariant();
        }

        private static string Truncate(string? value, int length) =>
            value == null ? "" : value.Length <= length ? value : value[..(length - 1)] + "…";
    }
}
