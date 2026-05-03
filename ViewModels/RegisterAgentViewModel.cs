using System.ComponentModel.DataAnnotations;

namespace SwiftPay.ViewModels
{
    public class RegisterAgentViewModel
    {
        [Required(ErrorMessage = "Store name is required")]
        public string StoreName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required(ErrorMessage = "Working hours are required")]
        public string WorkingHours { get; set; } = string.Empty;
    }
}
