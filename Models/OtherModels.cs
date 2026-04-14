// File: Models/TopUp.cs
namespace SwiftPay.Models
{
    public class TopUp
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string StripePaymentId { get; set; } = string.Empty;
        public TopUpStatus Status { get; set; } = TopUpStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public int AccountId { get; set; }

        // Navigation
        public Account Account { get; set; } = null!;
    }

    public enum TopUpStatus { Pending, Completed, Failed }
}


// File: Models/Agent.cs
namespace SwiftPay.Models
{
    public class Agent
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string WorkingHours { get; set; } = string.Empty;  // e.g. "Mon-Fri 9AM-6PM"
        public AgentStatus Status { get; set; } = AgentStatus.Pending;
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        // Foreign Key
        public string UserId { get; set; } = string.Empty;

        // Navigation
        public ApplicationUser User { get; set; } = null!;
    }

    public enum AgentStatus { Pending, Approved, Rejected }
}


// File: Models/CommissionRate.cs
namespace SwiftPay.Models
{
    public class CommissionRate
    {
        public int Id { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal Rate { get; set; }  // e.g. 1.8 means 1.8%
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}


// File: Models/Currency.cs
namespace SwiftPay.Models
{
    public class Currency
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;  // e.g. USD
        public string Name { get; set; } = string.Empty;  // e.g. US Dollar
        public string Symbol { get; set; } = string.Empty;  // e.g. $
        public string Flag { get; set; } = string.Empty;  // e.g. 🇺🇸
        public bool IsActive { get; set; } = true;
    }
}


// File: Models/Review.cs
namespace SwiftPay.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int Rating { get; set; }  // 1–5
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public string UserId { get; set; } = string.Empty;

        // Navigation
        public ApplicationUser User { get; set; } = null!;
    }
}


// File: Models/Notification.cs
namespace SwiftPay.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public NotificationType Type { get; set; } = NotificationType.TransferUpdate;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public string UserId { get; set; } = string.Empty;

        // Navigation
        public ApplicationUser User { get; set; } = null!;
    }

    public enum NotificationType
    {
        TransferUpdate,
        TopUp,
        AccountCreated,
        AgentApproval,
        General
    }
}
