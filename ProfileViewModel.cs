using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class ProfileViewModel
    {
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = string.Empty;

        [Display(Name = "Account created")]
        public DateTime? CreatedAt { get; set; }
    }
}