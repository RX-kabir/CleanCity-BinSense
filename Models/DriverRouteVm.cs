namespace SmartWaste.Web.Models
{
    public class DriverRouteRowVm
    {
        public int RouteId { get; set; }
        public DateTime RouteDate { get; set; }
        public byte RouteStatusId { get; set; }
        public string RouteStatus { get; set; } = "";
        public string RegistrationNo { get; set; } = "";
        public string DriverName { get; set; } = "";
        public int TotalStops { get; set; }
        public int CompletedStops { get; set; }
    }

    public class DriverRouteDetailsVm
    {
        public DriverRouteRowVm Header { get; set; } = new();
        public List<RouteStopMapDto> Stops { get; set; } = new(); // reuse your existing DTO
    }

    public class DriverLogPickupVm
    {
        public long RouteStopId { get; set; }
        public decimal VolumeCollectedLiters { get; set; }
        public int RouteId { get; set; }
        public string BinLocation { get; set; } = "";
    }
}
