// File: ViewModels/AdminDashboardViewModel.cs
namespace SwiftPay.ViewModels
{
    public class AdminDashboardViewModel
    {
        // KPI Cards
        public decimal TotalVolume { get; set; }
        public int VolumeChangePercent { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveAgents { get; set; }
        public int PendingAgents { get; set; }
        public decimal TotalCommission { get; set; }
        public int CommissionChangePercent { get; set; }

        // Pending agent approvals
        public List<AgentRequestViewModel> PendingAgentRequests { get; set; } = new();

        // Commission rates
        public List<CommissionRateViewModel> CommissionRates { get; set; } = new();

        // Recent transactions
        public List<AdminTransactionViewModel> RecentTransactions { get; set; } = new();
    }

    public class AgentRequestViewModel
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public DateTime AppliedDate { get; set; }
    }

    public class CommissionRateViewModel
    {
        public int Id { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public string FromFlag { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }

    public class AdminTransactionViewModel
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string FormattedAmount { get; set; } = string.Empty;
        public string CurrencyPair { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;  // approve / pending / rejected
        public DateTime Date { get; set; }
    }
}