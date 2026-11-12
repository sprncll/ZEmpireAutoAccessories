namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// Carrying what a quotation line says about film onto the job order line
    /// it became.
    ///
    /// The conversion itself is dbo.ConvertQuotationToJobOrder, the shop's own
    /// stored procedure and not mine to change. What it copies onto a job
    /// order line is whatever it was written to copy; if the tint variant, the
    /// shade and the panel are not among them, the job sheet for a converted
    /// quotation cannot say which film went on which glass - which is the one
    /// thing that line exists to say.
    ///
    /// So they are carried afterwards, and only where the job order line is
    /// plainly the quotation line it came from: same product or service, same
    /// price, same quantity, and the same NUMBER of each on both sides, paired
    /// in the order they were written. Anything less certain is left alone
    /// rather than guessed at - a panel copied onto the wrong line is worse
    /// than a panel missing, because it reads as a fact somebody recorded.
    ///
    /// Only ever fills a gap. Where the procedure already copies these, this
    /// finds nothing to do.
    /// </summary>
    public static class ConvertedLines
    {
        /// <summary>
        /// Fills in what the job order lines are missing. Returns whether
        /// anything was changed, so the caller only saves when there is
        /// something to save.
        /// </summary>
        public static bool Carry(
            IEnumerable<QuotationDetail> fromQuotation,
            IEnumerable<JobOrderDetail> onJobOrder)
        {
            var source = fromQuotation.OrderBy(d => d.QuotationDetailID).ToList();
            var target = onJobOrder.OrderBy(d => d.JobOrderDetailID).ToList();

            if (source.Count == 0 || target.Count == 0)
                return false;

            // Nothing to carry.
            if (!source.Any(d =>
                    d.TintVariantID != null || d.ShadeID != null || d.PanelID != null))
                return false;

            var changed = false;

            foreach (var group in source.GroupBy(d => new
            {
                d.ProductID,
                d.ServiceID,
                d.UnitPrice,
                d.Quantity
            }))
            {
                var lines = group.ToList();

                var matching = target
                    .Where(d =>
                        d.ProductID == group.Key.ProductID &&
                        d.ServiceID == group.Key.ServiceID &&
                        d.UnitPrice == group.Key.UnitPrice &&
                        d.Quantity == group.Key.Quantity)
                    .ToList();

                // One of these is not the other. Leave both as they are.
                if (matching.Count != lines.Count)
                    continue;

                for (var i = 0; i < lines.Count; i++)
                    changed |= CarryOne(lines[i], matching[i]);
            }

            return changed;
        }

        private static bool CarryOne(QuotationDetail from, JobOrderDetail to)
        {
            var changed = false;

            if (to.TintVariantID == null && from.TintVariantID != null)
            {
                to.TintVariantID = from.TintVariantID;
                changed = true;
            }

            if (to.ShadeID == null && from.ShadeID != null)
            {
                to.ShadeID = from.ShadeID;
                changed = true;
            }

            if (to.PanelID == null && from.PanelID != null)
            {
                to.PanelID = from.PanelID;
                changed = true;
            }

            // A film line counts panels, whatever unit the quotation happened
            // to be written in - the same rule AddLine keeps.
            if (to.PanelID != null
                && from.Unit == UnitOfMeasure.PanelUnit
                && to.Unit != UnitOfMeasure.PanelUnit)
            {
                to.Unit = UnitOfMeasure.PanelUnit;
                changed = true;
            }

            return changed;
        }
    }
}
