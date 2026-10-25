using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class CategoryViewModel
    {
        public int CategoryID { get; set; }

        public int EventID { get; set; }

        [Required(ErrorMessage = "Please enter a category name.")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.1, 1000, ErrorMessage = "Distance must be greater than zero.")]
        public double DistanceKm { get; set; }

        [Range(0, 1000000, ErrorMessage = "Entry fee cannot be negative.")]
        public decimal EntryFee { get; set; }

        [Range(1, 1000000, ErrorMessage = "Maximum participants must be at least 1.")]
        public int MaxParticipants { get; set; }
    }
}