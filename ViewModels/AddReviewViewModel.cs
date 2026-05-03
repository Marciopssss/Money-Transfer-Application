using System.ComponentModel.DataAnnotations;

namespace SwiftPay.ViewModels
{
    public class AddReviewViewModel
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Please write a comment")]
        [MaxLength(500)]
        public string Comment { get; set; } = string.Empty;
    }
}
