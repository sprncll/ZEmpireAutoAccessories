namespace ZEmpireAutoAccessories.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        /// <summary>The address that failed, e.g. "/Cashiering".</summary>
        public string? Path { get; set; }

        /// <summary>The exception's type, for Admin only. Null otherwise.</summary>
        public string? ErrorType { get; set; }

        /// <summary>The exception's message, for Admin only. Null otherwise.</summary>
        public string? ErrorMessage { get; set; }
    }
}
