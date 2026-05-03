using System.ComponentModel.DataAnnotations;

namespace SwiftPay.ViewModels
{

    public class TransactionHistoryViewModel
    {
        public List<TransactionRowViewModel> Transactions { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string? StatusFilter { get; set; }
        public string? TypeFilter { get; set; }
    }
}