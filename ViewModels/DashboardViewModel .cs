// File: ViewModels/DashboardViewModel.cs
using System.Globalization;

namespace SwiftPay.ViewModels
{
    public class DashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string AccountSerial { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string PreferredCurrency { get; set; } = "USD";
        public CultureInfo CultureInfo { get; set; } = CultureInfo.GetCultureInfo("en-US");

        public decimal MonthlySent { get; set; }
        public decimal MonthlyReceived { get; set; }
        public int SentChangePercent { get; set; }
        public int ReceivedChangePercent { get; set; }

        public int UnreadNotifications { get; set; }

        public string TimeOfDay =>
            DateTime.Now.Hour < 12 ? "morning" :
            DateTime.Now.Hour < 17 ? "afternoon" : "evening";

        public List<TransactionRowViewModel> RecentTransactions { get; set; } = new();
    }

    public class TransactionRowViewModel
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string FormattedAmount { get; set; } = string.Empty;
        public string AmountClass { get; set; } = string.Empty;  // send / recv / topup
        public string IconClass { get; set; } = string.Empty;
        public string IconSymbol { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;  // completed / pending / failed
        public DateTime Date { get; set; }
    }
}