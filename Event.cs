using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime EventDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public string Location { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int OrganiserID { get; set; }

        // URL of the event image stored in Azure Blob Storage
        public string? ImageUrl { get; set; }

        // Navigation properties
        public User? Organiser { get; set; }

        public ICollection<Category> Categories { get; set; }
            = new List<Category>();

        public ICollection<Enrolment> Enrolments { get; set; }
            = new List<Enrolment>();

        public Route? Route { get; set; }
    }
}