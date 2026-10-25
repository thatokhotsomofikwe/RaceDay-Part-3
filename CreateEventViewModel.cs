using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class CreateEventViewModel
    {
        [Required]
        [StringLength(150)]
        public string EventName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(7);

        [Required]
        public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0);

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Upcoming";
    }
}
