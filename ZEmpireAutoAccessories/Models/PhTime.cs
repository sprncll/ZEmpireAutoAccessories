namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// The time in the Philippines, whatever the server's clock says.
    ///
    /// DateTime.Now is the SERVER's local time, and the server is not in the
    /// shop: shared hosting runs on UTC or on US time, so a sale rung up at
    /// 1:34 in the afternoon was stamped 5:34 in the morning, and "today" on
    /// the dashboard and in Cashiering turned over at the wrong hour. Every
    /// date the system writes or compares now comes from here instead.
    ///
    /// The Philippines is UTC+8 all year - no daylight saving - so a fixed
    /// offset is exact; the named zone is used where the server has it.
    /// </summary>
    public static class PhTime
    {
        private static readonly TimeZoneInfo Zone = Find();

        private static TimeZoneInfo Find()
        {
            // Linux and newer Windows know the IANA name; older Windows only
            // its own, which for UTC+8 without DST is "Singapore Standard Time".
            foreach (var id in new[] { "Asia/Manila", "Singapore Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            return TimeZoneInfo.CreateCustomTimeZone("PHT", TimeSpan.FromHours(8), "Philippine Time", "Philippine Time");
        }

        /// <summary>The date and time in the Philippines now.</summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

        /// <summary>Today's date in the Philippines, at midnight.</summary>
        public static DateTime Today => Now.Date;
    }
}
