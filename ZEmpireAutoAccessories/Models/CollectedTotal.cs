namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// What was taken over a period, split by where it came from.
    ///
    /// The two halves are kept apart because they are counted differently. A
    /// product sale is money the moment it is recorded - sales.Sales has no
    /// status and a recorded sale is a completed one. A service invoice only
    /// counts once it is Paid; a pending or cancelled one is work, not
    /// takings. That is the same rule Cashiering closes a day out with, and
    /// the reason these figures agree with the Collections report.
    /// </summary>
    public class CollectedTotal
    {
        public decimal Sales { get; set; }
        public decimal ServiceInvoices { get; set; }

        public decimal Total => Sales + ServiceInvoices;
    }
}
