namespace SmartWaste.Web.Models
{
    public class RouteMapViewModel
    {
        public int RouteId { get; set; }
        public DateTime RouteDate { get; set; }

        public byte RouteStatusId { get; set; }
        public string RouteStatus { get; set; } = "";

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int TruckId { get; set; }
        public string RegistrationNo { get; set; } = "";

        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";

        public int TotalStops { get; set; }
        public int CompletedStops { get; set; }
    }
}
