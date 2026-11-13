using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// One thing somebody did: a record added, changed or deleted, or an
    /// action with no record of its own (a receipt attached, a sign-in).
    ///
    /// Written by <see cref="Services.ActivityRecorder"/> on every save, so
    /// a screen does not have to remember to log - a new screen is covered
    /// the day it is written. Read on the Activity Log screen, Admin only.
    /// </summary>
    [Table("ActivityLog", Schema = "sec")]
    public class ActivityLog
    {
        public const string Added = "Added";
        public const string Changed = "Changed";
        public const string Deleted = "Deleted";
        public const string Note = "Note";

        [Key]
        public long ActivityID { get; set; }

        public DateTime At { get; set; }

        /// <summary>Null for the site itself - the overnight expiry sweep.</summary>
        [MaxLength(450)]
        public string? UserId { get; set; }

        /// <summary>The sign-in name at the time, kept so a deleted account still reads.</summary>
        [MaxLength(256)]
        public string? UserName { get; set; }

        /// <summary>Added, Changed, Deleted or Note.</summary>
        [Required, MaxLength(20)]
        public string Action { get; set; } = Changed;

        /// <summary>What kind of record: "ServiceInvoice", "Customer"...</summary>
        [Required, MaxLength(100)]
        public string Entity { get; set; } = string.Empty;

        /// <summary>Which one: its id, as text so a composite key fits too.</summary>
        [MaxLength(100)]
        public string? EntityKey { get; set; }

        /// <summary>
        /// The document this record belongs to, when it is part of one: a
        /// payment or a line belongs to its invoice, a vehicle to its
        /// customer. So a document's history shows everything done to it,
        /// its parts included.
        /// </summary>
        [MaxLength(100)]
        public string? Parent { get; set; }

        [MaxLength(100)]
        public string? ParentKey { get; set; }

        /// <summary>One line a person can read: "Changed Service invoice INV-2026-000044".</summary>
        [Required, MaxLength(400)]
        public string Summary { get; set; } = string.Empty;

        /// <summary>Field by field, "Status: Pending → Paid", one per line.</summary>
        public string? Changes { get; set; }
    }
}
