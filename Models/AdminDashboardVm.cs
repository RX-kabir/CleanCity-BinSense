namespace SmartWaste.Web.Models
{
    public class AdminDashboardVm
    {
        public int TotalBins { get; set; }
        public int OkBins { get; set; }
        public int NeedsPickupBins { get; set; }
        public int OverflowingBins { get; set; }

        public int PickupsToday { get; set; }
        public decimal VolumeTodayLiters { get; set; }

        public int RoutesToday { get; set; }
        public int PlannedRoutes { get; set; }
        public int InProgressRoutes { get; set; }
        public int CompletedRoutes { get; set; }
        public int CancelledRoutes { get; set; }

        public List<PickupTrendPoint> Trend { get; set; } = new();
        public List<RecentPickupRow> RecentPickups { get; set; } = new();
    }

    public class PickupTrendPoint
    {
        public DateTime Day { get; set; }
        public int PickupsCount { get; set; }
        public decimal VolumeLiters { get; set; }
    }

    public class RecentPickupRow
    {
        public long PickupId { get; set; }
        public DateTime PickupTime { get; set; }
        public decimal VolumeCollectedLiters { get; set; }
        public int BinId { get; set; }
        public string Location { get; set; } = "";
        public string RegistrationNo { get; set; } = "";
        public string DriverName { get; set; } = "";
    }
}
