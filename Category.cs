using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public int EventID { get; set; }
        [ForeignKey("EventID")]
        public Event? Event { get; set; }

        [Required, StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(6,2)")]
        public decimal DistanceKm { get; set; }

        [Required, Column(TypeName = "decimal(10,2)")]
        public decimal EntryFee { get; set; }

        [Required]
        public int MaxParticipants { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}