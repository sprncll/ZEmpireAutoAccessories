using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ZEmpireAutoAccessories.Authorization
{
    /// <summary>
    /// The prices the catalogue holds - a product's or service's default price,
    /// and the rows of the pricing matrix - are administrators' to set.
    ///
    /// A member of staff who changes one, by accident or otherwise, moves what
    /// every later job and sale is quoted at, and there is nothing on the
    /// document afterwards to show it happened. The matrix at least journals
    /// the change to cat.PriceHistory; a default price does not, so there
    /// would be nothing to look back at.
    ///
    /// This is deliberately not a module of its own. Module access decides
    /// which screens someone can reach - staff still need to look prices up,
    /// and still need to type the price on a line where a discount or a quoted
    /// figure differs from the book. This decides only who may change what the
    /// book says.
    /// </summary>
    public sealed class AdminOnlyPriceAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.IsInRole(AppRoles.Admin))
                return;

            // Forbid rather than redirect to a sign-in page: the user is signed
            // in and known, they simply may not do this.
            context.Result = new ForbidResult();
        }
    }

    /// <summary>The two roles the shop uses.</summary>
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";

        /// <summary>
        /// Whether this user may change a price the catalogue holds. Used by
        /// the forms to render the field read-only, and by the controllers to
        /// ignore a posted value rather than trusting the form.
        /// </summary>
        public static bool CanEditCatalogPrices(System.Security.Claims.ClaimsPrincipal user) =>
            user.IsInRole(Admin);
    }
}
