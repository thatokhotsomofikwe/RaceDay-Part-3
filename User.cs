using RaceDay.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(150), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Role { get; set; } = string.Empty; // Organiser or Participant

        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
