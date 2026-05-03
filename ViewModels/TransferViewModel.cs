// File: ViewModels/TransferViewModel.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SwiftPay.ViewModels
{
    public class SendMoneyViewModel
    {
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 100000, ErrorMessage = "Amount must be between 0.01 and 100,000")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Please select a currency")]
        public string FromCurrency { get; set; } = "USD";

        [Required(ErrorMessage = "Please select a target currency")]
        public string ToCurrency { get; set; } = "USD";

        public TransferMethod Method { get; set; } = TransferMethod.Beneficiary;

        // For beneficiary transfer
        public int? BeneficiaryId { get; set; }

        // For mobile transfer
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? ReceiverMobileNumber { get; set; }
        public string? ReceiverName { get; set; }

        public string? Notes { get; set; }

        // Display info
        public decimal Balance { get; set; }
        public string AccountSerial { get; set; } = string.Empty;
        public decimal ExchangeRate { get; set; } = 1;
        public decimal ConvertedAmount { get; set; }
        public decimal CommissionAmount { get; set; }

        public List<SelectListItem> CurrencyOptions { get; set; } = new();
        public List<SelectListItem> BeneficiaryOptions { get; set; } = new();
    }

    public class TransferConfirmViewModel
    {
        public string SerialNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal CommissionAmount { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public enum TransferMethod { Beneficiary, Mobile }

    public class AddBeneficiaryViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? MobileNumber { get; set; }

        public string? AccountSerial { get; set; }
        public string? BankName { get; set; }
        public string? IBAN { get; set; }
    }
}