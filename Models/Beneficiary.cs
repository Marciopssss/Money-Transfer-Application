// File: Models/Beneficiary.cs
namespace SwiftPay.Models
{
    public class Beneficiary
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? MobileNumber { get; set; }
        public string? AccountSerial { get; set; }  // if sending to a SwiftPay account
        public string? BankName { get; set; }
        public string? IBAN { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public string UserId { get; set; } = string.Empty;

        // Navigation
        public ApplicationUser User { get; set; } = null!;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
