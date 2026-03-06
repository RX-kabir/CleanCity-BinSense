namespace SmartWaste.Web.Models
{
    public class RouteStopMapDto
    {
        public long RouteStopId { get; set; }
        public int RouteId { get; set; }
        public int StopOrder { get; set; }

        public DateTime? PlannedTime { get; set; }
        public DateTime? ActualTime { get; set; }
        public decimal? CollectedVolumeLiters { get; set; }

        public int BinId { get; set; }
        public string Location { get; set; } = "";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public byte BinStatusId { get; set; }
        public string StatusName { get; set; } = "";

        public int CapacityLiters { get; set; }
        public string WasteType { get; set; } = "";
    }
}
