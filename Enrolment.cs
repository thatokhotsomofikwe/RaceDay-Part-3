using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        [Required]
        public int EventID { get; set; }
        [ForeignKey("EventID")]
        public Event? Event { get; set; }

        [Required]
        public int CategoryID { get; set; }
        [ForeignKey("CategoryID")]
        public Category? Category { get; set; }

        [Required]
        public int ParticipantID { get; set; }
        [ForeignKey("ParticipantID")]
        public User? Participant { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [Required, StringLength(20)]
        public string Status { get; set; } = "Confirmed"; // Confirmed, Cancelled, Completed

        public Result? Result { get; set; }
    }
}
