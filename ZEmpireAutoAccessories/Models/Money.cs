using System.Globalization;

namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// Pesos as the shop writes them: the groups of three separated by a
    /// comma - ₱123,456,789.00.
    ///
    /// Only for displaying a figure. Nothing here parses one back: a posted
    /// amount is still read with the request's own culture, so this cannot
    /// change what a typed figure means.
    ///
    /// Every figure on the cashiering, sales, job order and service invoice
    /// screens comes through here, so what a peso looks like is this one line
    /// rather than forty-one of them.
    /// </summary>
    public static class Money
    {
        private const string GroupSeparator = ",";

        private static readonly NumberFormatInfo Pesos = new()
        {
            NumberGroupSeparator = GroupSeparator,
            NumberDecimalSeparator = ".",
            NumberGroupSizes = new[] { 3 },
            NumberDecimalDigits = 2,
            // The sign goes before the peso sign, not between it and the
            // digits: a refund reads -₱500.00, never ₱-500.00.
            NumberNegativePattern = 1
        };

        /// <summary>"₱123,456,789.00", or "-₱500.00" for a negative.</summary>
        public static string Peso(decimal amount) =>
            amount < 0
                ? "-₱" + (-amount).ToString("N2", Pesos)
                : "₱" + amount.ToString("N2", Pesos);

        /// <summary>
        /// The same, for an amount that may not have been recorded. An em
        /// dash rather than ₱0.00, because nothing paid and zero paid are
        /// different facts.
        /// </summary>
        public static string Peso(decimal? amount, string whenNull = "—") =>
            amount.HasValue ? Peso(amount.Value) : whenNull;

        /// <summary>
        /// The figure without the peso sign, for a column that carries the
        /// sign in its heading instead of on every row.
        /// </summary>
        public static string Plain(decimal amount) => amount.ToString("N2", Pesos);
    }
}
