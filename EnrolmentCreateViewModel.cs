namespace RaceDay.MVC.Models
{
    public class EnrolmentCreateViewModel
    {
        public int EventID { get; set; }

        public string EventName { get; set; } = string.Empty;

        public int CategoryID { get; set; }

        public List<CategoryOptionViewModel> Categories { get; set; }
            = new List<CategoryOptionViewModel>();
    }

    public class CategoryOptionViewModel
    {
        public int CategoryID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public double DistanceKm { get; set; }

        public decimal EntryFee { get; set; }

        public int MaxParticipants { get; set; }
    }
}
