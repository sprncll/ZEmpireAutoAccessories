using Microsoft.AspNetCore.Identity;

namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// Identity user mapped to asp.AspNetUsers. Adds the application-specific
    /// columns that live alongside the standard Identity columns.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = PhTime.Now;
    }
}
