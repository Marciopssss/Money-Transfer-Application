
namespace SwiftPay.ViewModels
{
    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string PreferredCurrency { get; set; } = "USD";
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> CurrencyOptions { get; set; } = new();
    }
}