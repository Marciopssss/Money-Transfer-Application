// File: ViewModels/AccountViewModels.cs
using System.ComponentModel.DataAnnotations;

namespace SwiftPay.ViewModels
{
    public class SettingsViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        [Required]
        public string PreferredCurrency { get; set; } = "USD";

        // Password change (optional)
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string? ConfirmNewPassword { get; set; }

        // Display only
        public string AccountSerial { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime MemberSince { get; set; }
    }

    public class ExchangeViewModel
    {
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 100000, ErrorMessage = "Amount must be between 0.01 and 100,000")]
        public decimal Amount { get; set; }

        [Required]
        public string FromCurrency { get; set; } = "USD";

        [Required]
        public string ToCurrency { get; set; } = "EUR";

        // Result
        public decimal ExchangeRate { get; set; }
        public decimal ConvertedAmount { get; set; }
        public decimal Balance { get; set; }
        public string AccountSerial { get; set; } = string.Empty;
        public bool Exchanged { get; set; } = false;
    }
}