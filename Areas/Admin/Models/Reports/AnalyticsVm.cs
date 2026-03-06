namespace SmartWaste.Web.Areas.Admin.Models.Reports
{
    public class AnalyticsVm
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // KPI Cards
        public int TotalPickups { get; set; }
        public decimal TotalCollectedLiters { get; set; }
        public int OverflowAlerts { get; set; }
        public int TotalRoutes { get; set; }

        // Trend
        public List<DailyTrendRow> Trend { get; set; } = new();

        // Top Zones
        public List<TopZoneRow> TopZones { get; set; } = new();

        // Top Overflow Bins
        public List<TopOverflowBinRow> TopOverflowBins { get; set; } = new();

        // Truck Utilization
        public List<TruckUtilRow> TruckUtilization { get; set; } = new();
    }

    public class DailyTrendRow
    {
        public DateTime Day { get; set; }
        public int PickupCount { get; set; }
        public decimal CollectedLiters { get; set; }
    }

    public class TopZoneRow
    {
        public string ZoneName { get; set; } = "";
        public decimal TotalCollectedLiters { get; set; }
        public int PickupCount { get; set; }
    }

    public class TopOverflowBinRow
    {
        public int BinId { get; set; }
        public string Location { get; set; } = "";
        public string ZoneName { get; set; } = "";
        public int OverflowCount { get; set; }
    }

    public class TruckUtilRow
    {
        public string RegistrationNo { get; set; } = "";
        public int Routes { get; set; }
        public int TotalStops { get; set; }
        public int CompletedStops { get; set; }
    }
}
