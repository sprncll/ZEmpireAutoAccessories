namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// How a dashboard figure should read at a glance. A count of low-stock
    /// items is not the same kind of number as the day's takings, and showing
    /// both in the same grey was the old dashboard's main failing: nothing on
    /// it ever looked like it needed doing.
    /// </summary>
    public enum StatTone
    {
        /// <summary>Just a figure. Takings, totals, anything purely informational.</summary>
        Plain = 0,

        /// <summary>Work waiting. Pending job orders, unpaid invoices.</summary>
        Waiting = 1,

        /// <summary>Something is wrong and someone should look. Stock running out.</summary>
        Attention = 2
    }

    /// <summary>
    /// One figure on the dashboard. <paramref name="Tone"/> defaults to Plain,
    /// so a stat added without thinking about it reads as information rather
    /// than falsely as an alarm.
    /// </summary>
    public record DashboardStat(
        string Label,
        string Value,
        string Link,
        string Icon,
        StatTone Tone = StatTone.Plain,
        string? Note = null);
}
