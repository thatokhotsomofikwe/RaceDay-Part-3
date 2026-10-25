using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Route
    {
        [Key]
        public int RouteID { get; set; }

        [Required]
        public int EventID { get; set; }
        [ForeignKey("EventID")]
        public Event? Event { get; set; }

        [Required, StringLength(150)]
        public string RouteName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(6,2)")]
        public decimal? DistanceKm { get; set; }

        [StringLength(500)]
        public string? RouteUrl { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
