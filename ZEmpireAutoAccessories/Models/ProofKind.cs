namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// What a stored file is evidence of. The kind and the record's id are the
    /// whole of a proof's identity - together they name the file on disk - so
    /// a sale and an invoice that happen to share an id never collide.
    /// </summary>
    public enum ProofKind
    {
        /// <summary>Payment for a product sale (sales.Sales).</summary>
        Sale = 0,

        /// <summary>Payment for a service invoice (sales.ServiceInvoice).</summary>
        ServiceInvoice = 1,

        /// <summary>
        /// That a warranty was claimed (sales.Warranty) - the signed claim
        /// slip, the replacement receipt, or a photo of the work redone.
        /// </summary>
        WarrantyClaim = 2
    }

    public static class ProofKinds
    {
        /// <summary>
        /// The kind of proof a cashiering row carries. Cashiering merges two
        /// tables; a proof is filed against whichever one the row came from.
        /// </summary>
        public static ProofKind For(CashierSource source) =>
            source == CashierSource.Sale ? ProofKind.Sale : ProofKind.ServiceInvoice;
    }
}
