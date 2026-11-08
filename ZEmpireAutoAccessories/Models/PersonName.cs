namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// A customer's name as two boxes on a form, out of the one column the
    /// database has.
    ///
    /// crm.Customer holds a single FullName, and it is what every list,
    /// dropdown, receipt and PDF in the system already prints. Splitting the
    /// column would mean a schema change and rewriting all of them, so the
    /// column stays as it is and the form does the splitting.
    ///
    /// Stored first name then last name - "Juan Dela Cruz" - because that is
    /// how every row already in the table reads and how the shop says a name
    /// aloud. The boxes are ordered last name first, which is how a customer
    /// gives it at a counter, but what goes into the column is unchanged.
    /// </summary>
    public static class PersonName
    {
        /// <summary>
        /// The two boxes as the one stored name. Extra spaces between or
        /// around the parts are collapsed, so "Juan " and " Dela  Cruz" still
        /// store as "Juan Dela Cruz".
        /// </summary>
        public static string Combine(string? firstName, string? lastName)
        {
            var parts = $"{firstName?.Trim()} {lastName?.Trim()}"
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return string.Join(" ", parts);
        }

        /// <summary>
        /// The stored name back into the two boxes, for the edit form.
        ///
        /// The FIRST word is the first name and everything after it is the
        /// last name, which is the right way round here: Filipino surnames
        /// are routinely two or three words - Dela Cruz, De los Santos - and
        /// first names are rarely more than one. "Juan Dela Cruz" comes back
        /// as "Juan" and "Dela Cruz", and goes back in the same way.
        ///
        /// A name of one word is a first name with no surname on file rather
        /// than a surname with no first name, so nothing is invented.
        /// </summary>
        public static (string First, string Last) Split(string? fullName)
        {
            var parts = (fullName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length switch
            {
                0 => (string.Empty, string.Empty),
                1 => (parts[0], string.Empty),
                _ => (parts[0], string.Join(" ", parts.Skip(1)))
            };
        }
    }
}
