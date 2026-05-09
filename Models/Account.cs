namespace SwiftPay.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty; 
        public string Currency { get; set; } = "USD";
        public decimal Balance { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;
        public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
        public ICollection<TopUp> TopUps { get; set; } = new List<TopUp>();


    }
}
