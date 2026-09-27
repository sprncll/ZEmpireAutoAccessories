namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// Philippine VAT, at 12%.
    ///
    /// Every price in the shop's list is what the customer pays - "Small -
    /// P9,900" is the figure on the board and the figure handed over, so the
    /// VAT is already inside it. That makes the breakdown a division, not an
    /// addition: a P9,900 job is P8,839.29 of VATable sales plus P1,060.71 of
    /// VAT. Charging 12% on top would quietly raise every price by 12%.
    ///
    /// Nothing here is stored. sales.Sales has no tax column at all, and on
    /// sales.ServiceInvoice CK_ServiceInvoice_Math reads
    /// TotalAmount = SubTotal - DiscountAmount + TaxAmount, so anything put in
    /// TaxAmount is added to what the customer owes - the opposite of an
    /// inclusive price. The breakdown is therefore worked out from the total
    /// whenever a document is shown, which is also how a BIR sales invoice
    /// prints it.
    /// </summary>
    public static class Vat
    {
        /// <summary>The rate, as a fraction.</summary>
        public const decimal Rate = 0.12m;

        /// <summary>The rate as it is written on a receipt.</summary>
        public const string RateLabel = "12%";

        /// <summary>
        /// The part of a VAT-inclusive total that is the sale itself, to the
        /// centavo. Rounded away from zero, the way a till rounds.
        /// </summary>
        public static decimal VatableSales(decimal total) =>
            total <= 0m
                ? 0m
                : decimal.Round(total / (1m + Rate), 2, MidpointRounding.AwayFromZero);

        /// <summary>
        /// The VAT inside a VAT-inclusive total. Taken as the remainder rather
        /// than as its own rounded multiplication, so the two figures always
        /// add back up to the total exactly.
        /// </summary>
        public static decimal Amount(decimal total) =>
            total <= 0m ? 0m : total - VatableSales(total);

        /// <summary>Both halves of a VAT-inclusive total in one call.</summary>
        public static (decimal VatableSales, decimal Amount) Breakdown(decimal total)
        {
            var vatable = VatableSales(total);
            return (vatable, total <= 0m ? 0m : total - vatable);
        }
    }
}
