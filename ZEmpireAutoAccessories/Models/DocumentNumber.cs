namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// The number printed on a job order or a quotation: a prefix saying what
    /// the document is, the year it was raised, and a sequence.
    ///
    ///     JO-2026-00047        QT-2026-00112
    ///
    /// The sequence is the row's own identity key rather than a count of the
    /// documents raised this year. Counting would read the table and add one,
    /// and two people saving in the same moment would both read the same
    /// figure and both write it - a duplicate number on two different cars,
    /// found weeks later when somebody looks for one and gets two. The
    /// identity key is the database's to hand out and it never repeats.
    ///
    /// The cost is that the sequence does not restart in January: the first
    /// job order of 2027 carries on from the last of 2026. The year still
    /// says when the work was raised, which is what the number is read for.
    ///
    /// Documents raised before this keep the numbers they were given. A
    /// number identifies one document for as long as it exists, and
    /// renumbering history would break every receipt already in a customer's
    /// hands.
    ///
    /// Service invoices are NOT numbered here. Theirs come from
    /// dbo.GetNextInvoiceNumber, a stored procedure with its own series
    /// table, which is the shop's own numbering and not mine to change.
    /// </summary>
    public static class DocumentNumber
    {
        public const string JobOrder = "JO";
        public const string Quotation = "QT";

        public static string For(string prefix, int id, DateTime raisedOn) =>
            $"{prefix}-{raisedOn:yyyy}-{id:D5}";
    }
}
