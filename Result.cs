using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public int EnrolmentID { get; set; }
        [ForeignKey("EnrolmentID")]
        public Enrolment? Enrolment { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Finished"; // Finished, DidNotFinish, Disqualified

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}