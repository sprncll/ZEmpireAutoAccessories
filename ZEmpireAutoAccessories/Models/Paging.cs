using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace ZEmpireAutoAccessories.Models
{
    /// <summary>Where a paged list is: which page, of how many, of how many rows.</summary>
    public sealed record PageInfo(int Page, int PageSize, int Total)
    {
        public int Pages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
        public int First => Total == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int Last => Math.Min(Page * PageSize, Total);
        public bool HasMore => Pages > 1;
    }

    /// <summary>
    /// One page of a list at a time.
    ///
    /// The lists loaded every row they had - fine at fifty, slow at five
    /// thousand, and on shared hosting with 256 MB the whole table is held
    /// in memory to draw a screen that shows the first screenful. A list is
    /// now read fifty rows at a time, with a count for the bar under it.
    ///
    /// Search and filters still run against every row: the page is cut from
    /// what matched, not the other way round.
    /// </summary>
    public static class Paging
    {
        public const int PageSize = 50;
        public const string Key = "Paging";

        /// <summary>
        /// Counts what the query matches, then reads the asked-for page of it.
        /// A page past the end is taken as the last one, so a link left over
        /// from a longer list still lands somewhere.
        /// </summary>
        public static async Task<List<T>> ToPageAsync<T>(
            this IQueryable<T> query, int? page, ViewDataDictionary viewData)
        {
            var total = await query.CountAsync();
            var info = Clamp(page, total);
            viewData[Key] = info;

            return await query
                .Skip((info.Page - 1) * info.PageSize)
                .Take(info.PageSize)
                .ToListAsync();
        }

        /// <summary>The same, for a list already in memory.</summary>
        public static List<T> ToPage<T>(this IList<T> rows, int? page, ViewDataDictionary viewData)
        {
            var info = Clamp(page, rows.Count);
            viewData[Key] = info;
            return rows.Skip((info.Page - 1) * info.PageSize).Take(info.PageSize).ToList();
        }

        private static PageInfo Clamp(int? page, int total)
        {
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            return new PageInfo(Math.Clamp(page ?? 1, 1, pages), PageSize, total);
        }
    }
}
