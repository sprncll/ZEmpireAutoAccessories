using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.TagHelpers
{
    /// <summary>
    /// Put <c>zx-return</c> on a link that leaves this screen for another,
    /// and the link carries where it came from, so the Back button over there
    /// comes back here instead of dumping somebody on a list.
    ///
    ///     &lt;a asp-controller="Warranty" asp-action="Create" zx-return&gt;
    ///
    /// Nothing else is needed: <see cref="BackLink"/> reads it on the other
    /// side. Deliberately opt-in rather than applied to every link on the
    /// site - the menu and the dashboard are how somebody LEAVES what they
    /// were doing, and a Back button that undoes that is not a Back button.
    ///
    /// Runs after the built-in anchor tag helper (which is Order -1000), so
    /// the href it reads is the finished one.
    /// </summary>
    [HtmlTargetElement("a", Attributes = AttributeName)]
    public class ReturnUrlTagHelper : TagHelper
    {
        private const string AttributeName = "zx-return";

        public override int Order => 0;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; } = null!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.RemoveAll(AttributeName);

            if (!output.Attributes.TryGetAttribute("href", out var href))
                return;

            var url = href.Value?.ToString();

            // Only a path on this site, and never one that already says where
            // it came from.
            if (!BackLink.IsSafe(url)
                || url!.Contains(BackLink.Key + "=", StringComparison.OrdinalIgnoreCase))
                return;

            var here = BackLink.Here(ViewContext.HttpContext.Request);

            // A link back to the screen it is on would be a Back button that
            // goes nowhere.
            if (string.Equals(url, here, StringComparison.OrdinalIgnoreCase))
                return;

            var separator = url.Contains('?') ? "&" : "?";

            output.Attributes.SetAttribute("href",
                url + separator + BackLink.Key + "=" + Uri.EscapeDataString(here));
        }
    }
}
