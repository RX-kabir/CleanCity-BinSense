namespace SmartWaste.Web.Areas.Admin.Models.Reports
{
    public class TruckRouteRow
    {
        public string TruckNo { get; set; } = "";
        public DateTime RouteDate { get; set; }
        public string RouteStatus { get; set; } = "";
        public int TotalStops { get; set; }
        public int CompletedStops { get; set; }
    }
}
