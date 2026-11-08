namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// Philippine mobile numbers, stored in one shape: "+63" then ten digits
    /// beginning 9 - "+639121231231".
    ///
    /// The shop dictates the number aloud as "oh nine one two...", pastes it
    /// from Messenger as "+63 912 123 1231", and reads it off a card as
    /// "0912-123-1231". All three are the same number, so all three are
    /// accepted and normalised here rather than refused at the form.
    /// </summary>
    public static class PhoneNumber
    {
        /// <summary>
        /// The stored form of whatever was typed, or null if it is not a
        /// Philippine mobile number at all.
        /// </summary>
        public static string? ToPhilippineMobile(string? typed)
        {
            if (string.IsNullOrWhiteSpace(typed))
                return null;

            var digits = new string(typed.Where(char.IsDigit).ToArray());

            // Strip whichever prefix was used, leaving the ten national digits.
            if (digits.Length == 12 && digits.StartsWith("63"))
                digits = digits[2..];           // 639121231231, or +63 912 123 1231
            else if (digits.Length == 11 && digits[0] == '0')
                digits = digits[1..];           // 09121231231

            return digits.Length == 10 && digits[0] == '9'
                ? "+63" + digits
                : null;
        }

        /// <summary>
        /// The same number written the old way, "09" and nine more digits, so
        /// a search for one customer also finds the rows entered before the
        /// shop moved to +63. Null if the number is not a valid one.
        ///
        /// Both forms have to be looked for: a row holding 09121231231 and a
        /// row holding +639121231231 are the same person's number, and a
        /// uniqueness check comparing only the stored shape would let the
        /// second be entered beside the first.
        /// </summary>
        public static string? ToDialledForm(string? typed)
        {
            var normalised = ToPhilippineMobile(typed);
            return normalised is null ? null : "0" + normalised[3..];
        }

        /// <summary>
        /// The ten digits a form shows beside a fixed "+63", from whatever is
        /// stored. Older rows hold "09..."; they display the same way.
        /// </summary>
        public static string ToLocalDigits(string? stored)
        {
            var normalised = ToPhilippineMobile(stored);
            return normalised is null ? string.Empty : normalised[3..];
        }
    }
}
