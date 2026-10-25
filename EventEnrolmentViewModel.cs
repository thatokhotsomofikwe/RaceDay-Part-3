using System;

namespace RaceDay.MVC.Models
{
    public class EventEnrolmentViewModel
    {
        public int EnrolmentID { get; set; }

        public int ParticipantID { get; set; }

        public string ParticipantName { get; set; } = string.Empty;

        public int CategoryID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public DateTime EnrolmentDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}