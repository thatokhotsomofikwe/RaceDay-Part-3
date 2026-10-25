using System;

namespace RaceDay.MVC.Models
{
    public class ResultViewModel
    {
        public int ResultID { get; set; }

        public int EnrolmentID { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string ParticipantName { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime RecordedAt { get; set; }
    }
}