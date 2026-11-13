using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services;

namespace ZEmpireAutoAccessories.Controllers
{
    /// <summary>
    /// Who changed what. Admin only: it names every user and shows every
    /// figure anyone has changed, including the ones Staff are not shown.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ActivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SchemaStatus _schema;

        public ActivityController(ApplicationDbContext context, SchemaStatus schema)
        {
            _context = context;
            _schema = schema;
        }

        // GET: Activity?from=&to=&user=&entity=&key=&q=&page=
        public async Task<IActionResult> Index(
            DateOnly? from, DateOnly? to, string? user, string? entity, string? key, string? q, int? page)
        {
            ViewData["From"] = from;
            ViewData["To"] = to;
            ViewData["User"] = user;
            ViewData["Entity"] = entity;
            ViewData["Key"] = key;
            ViewData["Search"] = q;

            if (!await _schema.Has(_context, SchemaStatus.ActivityLog))
            {
                ViewData["NotInstalled"] = true;
                return View(new List<ActivityLog>());
            }

            var query = _context.ActivityLogs.AsNoTracking().AsQueryable();

            if (from.HasValue)
                query = query.Where(a => a.At >= from.Value.ToDateTime(TimeOnly.MinValue));
            if (to.HasValue)
                query = query.Where(a => a.At < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
            if (!string.IsNullOrEmpty(user))
                query = query.Where(a => a.UserName == user);
            if (!string.IsNullOrEmpty(entity) && !string.IsNullOrEmpty(key))
                // One record's history: the record itself, and everything that
                // is part of it - an invoice's lines and payments.
                query = query.Where(a => (a.Entity == entity && a.EntityKey == key)
                                      || (a.Parent == entity && a.ParentKey == key));
            else if (!string.IsNullOrEmpty(entity))
                query = query.Where(a => a.Entity == entity);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(a => a.Summary.Contains(term)
                                      || (a.Changes != null && a.Changes.Contains(term))
                                      || (a.UserName != null && a.UserName.Contains(term)));
            }

            var rows = await query
                .OrderByDescending(a => a.At)
                .ThenByDescending(a => a.ActivityID)
                .ToPageAsync(page, ViewData);

            // The people and the kinds of record that appear in the log at
            // all, for the two filters.
            ViewData["Users"] = new SelectList(
                await _context.ActivityLogs.Where(a => a.UserName != null)
                    .Select(a => a.UserName!).Distinct().OrderBy(n => n).ToListAsync(),
                user);

            ViewData["Entities"] = (await _context.ActivityLogs
                    .Select(a => a.Entity).Distinct().ToListAsync())
                .Select(e => new SelectListItem(Capitalised(ActivityRecorder.Friendly(e)), e, e == entity))
                .OrderBy(i => i.Text)
                .ToList();

            return View(rows);
        }

        private static string Capitalised(string text) =>
            text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
    }
}
