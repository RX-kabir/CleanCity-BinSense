namespace SmartWaste.Web.Areas.Admin.Models.Reports
{
    public class ZoneWasteRow
    {
        public string ZoneName { get; set; } = "";
        public DateTime ReportDate { get; set; }
        public decimal TotalCollectedLiters { get; set; }
        public int PickupCount { get; set; }
    }
}
