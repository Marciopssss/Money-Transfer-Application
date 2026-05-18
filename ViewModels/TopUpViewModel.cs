using System.ComponentModel.DataAnnotations;

namespace SwiftPay.ViewModels
{
    public class TopUpViewModel
    {
        [Required(ErrorMessage = "Amount is required")]
        [Range(1, 50000, ErrorMessage = "Amount must be between $1 and $50,000")]
        public decimal Amount { get; set; }

        public string? StripeToken { get; set; }
        public string? CardNumber { get; set; }
        public string? CardExpiry { get; set; }
        public string? CardCvc { get; set; }
    }
}
