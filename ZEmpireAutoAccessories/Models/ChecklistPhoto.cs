namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// One photo filed against a vehicle checklist. The slot is what the
    /// screens address it by, and it is a small whole number rather than a
    /// file name, so nothing a camera or a phone called the file reaches the
    /// disk or a URL.
    /// </summary>
    public record ChecklistPhoto(int Slot, string ContentType, long Bytes, DateTime TakenOn)
    {
        /// <summary>"2.4 MB", for a caption under the thumbnail.</summary>
        public string Size =>
            Bytes >= 1024 * 1024
                ? $"{Bytes / 1024m / 1024m:N1} MB"
                : $"{Bytes / 1024m:N0} KB";
    }
}
