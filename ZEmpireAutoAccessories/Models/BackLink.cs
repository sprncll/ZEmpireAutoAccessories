using Microsoft.AspNetCore.Http;

namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// Where a Back button goes.
    ///
    /// A screen reached from a list belongs to that list, and Back has always
    /// gone there. But half the screens in this system are reached from
    /// somewhere else: a job order opens the vehicle checklist, an invoice
    /// opens a warranty, the inventory list opens a product. Back then put
    /// somebody on a list they had never been looking at, and the way home
    /// was the browser's own button or the menu - which is how people end up
    /// re-finding the job order they were halfway through.
    ///
    /// So a link that crosses from one screen to another says where it came
    /// from, in a "returnUrl" on the query string, and the Back button on the
    /// other side returns there. Nothing says where it came from, nothing
    /// changes: Back still goes to the list, and still says "Back to list".
    ///
    /// A returnUrl is only ever followed when it is a path on this site. One
    /// that is not - anything starting "//", "/\" or a scheme - is dropped on
    /// the floor, because a Back button that can be pointed at another site
    /// by editing the address bar is an open redirect, and this system has a
    /// login page worth phishing.
    /// </summary>
    public static class BackLink
    {
        public const string Key = "returnUrl";

        /// <summary>A path on this site, and nothing else.</summary>
        public static bool IsSafe(string? url) =>
            !string.IsNullOrWhiteSpace(url)
            && url.Length <= 2048
            && url[0] == '/'
            && !url.StartsWith("//", StringComparison.Ordinal)
            && !url.StartsWith("/\\", StringComparison.Ordinal);

        /// <summary>
        /// The returnUrl this request carries, if it carries a usable one.
        /// Read from the query string, and from a posted form where a page
        /// carries it through a save.
        /// </summary>
        public static string? Given(HttpRequest? request)
        {
            if (request == null)
                return null;

            var fromQuery = request.Query[Key].ToString();
            if (IsSafe(fromQuery))
                return fromQuery;

            if (request.HasFormContentType)
            {
                var fromForm = request.Form[Key].ToString();
                if (IsSafe(fromForm))
                    return fromForm;
            }

            return null;
        }

        /// <summary>
        /// Where a Back button on this screen should point: where the visitor
        /// came from, or the fallback the screen names - its own list.
        /// </summary>
        public static string For(HttpRequest? request, string? fallback) =>
            Given(request) ?? (string.IsNullOrWhiteSpace(fallback) ? "/" : fallback);

        /// <summary>
        /// What that button should say. "Back to list" is a promise about
        /// where it goes, so it is only made when that is where it goes.
        /// </summary>
        public static string Label(HttpRequest? request, string whenList = "Back to list") =>
            Given(request) != null ? "Back" : whenList;

        /// <summary>
        /// This screen, as a returnUrl for the screen it is about to open.
        ///
        /// The query string comes too, so a search or a filter is still there
        /// on the way back. A chain of them is kept while it stays short -
        /// three screens deep is a person retracing their steps, and a
        /// kilometre of nested addresses is a bug.
        /// </summary>
        public static string Here(HttpRequest? request)
        {
            if (request == null)
                return "/";

            var path = request.Path.HasValue ? request.Path.Value! : "/";
            var query = request.QueryString.HasValue ? request.QueryString.Value! : string.Empty;

            var whole = path + query;
            if (whole.Length <= 600)
                return whole;

            // Too long to carry. Keep this screen, drop the trail behind it.
            var trimmed = QueryHelpers(request);
            return trimmed.Length <= 600 ? trimmed : path;
        }

        /// <summary>This screen's address without any returnUrl of its own.</summary>
        private static string QueryHelpers(HttpRequest request)
        {
            var path = request.Path.HasValue ? request.Path.Value! : "/";

            var kept = request.Query
                .Where(p => !string.Equals(p.Key, Key, StringComparison.OrdinalIgnoreCase))
                .SelectMany(p => p.Value.Select(v => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(v ?? string.Empty)}"))
                .ToList();

            return kept.Count == 0 ? path : path + "?" + string.Join("&", kept);
        }
    }
}
