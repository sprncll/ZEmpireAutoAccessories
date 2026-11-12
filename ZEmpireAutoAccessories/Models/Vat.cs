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
        ///
        /// <remarks>
        /// Twelve per cent since 2006, and not a programmer's to decide: BIR
        /// sets it and has moved it before. Set from the settings screen at
        /// startup and whenever it is saved - see <see cref="Configure"/>.
        /// </remarks>
        public static decimal Rate { get; private set; } = 0.12m;

        /// <summary>The rate as it is written on a receipt.</summary>
        public static string RateLabel { get; private set; } = "12%";

        /// <summary>
        /// Points the rate at what the shop has set. A rate outside 0 to 1 is
        /// ignored rather than applied: a receipt is worse for being wrong
        /// than for being out of date, and every figure on every document
        /// goes through here.
        ///
        /// The label is worked out from the rate rather than typed, so the
        /// two can never disagree - a receipt reading "VAT (12%)" beside an
        /// 8% division is how a dispute starts.
        /// </summary>
        public static void Configure(decimal rate)
        {
            if (rate < 0m || rate > 1m)
                return;

            Rate = rate;
            RateLabel = (rate * 100m).ToString("0.##") + "%";
        }

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
