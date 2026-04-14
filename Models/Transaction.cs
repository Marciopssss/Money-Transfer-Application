// File: Models/Transaction.cs
namespace SwiftPay.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;  // e.g. TXN-2026-00391

        public decimal Amount { get; set; }
        public decimal ConvertedAmount { get; set; }   // amount in receiver's currency
        public decimal ExchangeRate { get; set; } = 1;
        public decimal CommissionAmount { get; set; } = 0;

        public string FromCurrency { get; set; } = "USD";
        public string ToCurrency { get; set; } = "USD";

        public TransactionType Type { get; set; } = TransactionType.AccountToAccount;
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        // For OMT-style (mobile number transfer)
        public string? ReceiverMobileNumber { get; set; }
        public string? ReceiverName { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Foreign Keys
        public int SenderAccountId { get; set; }
        public int? ReceiverAccountId { get; set; }   // null for mobile transfers
        public int? BeneficiaryId { get; set; }

        // Navigation
        public Account SenderAccount { get; set; } = null!;
        public Account? ReceiverAccount { get; set; }
        public Beneficiary? Beneficiary { get; set; }
    }

    public enum TransactionType
    {
        AccountToAccount,
        MobileTransfer
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }
}
