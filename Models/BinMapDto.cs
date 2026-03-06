namespace SmartWaste.Web.Models
{
    public class BinMapDto
    {
        public int BinId { get; set; }
        public string Location { get; set; } = "";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public byte BinStatusId { get; set; }
        public string StatusName { get; set; } = "";

        public int LatestFillLevelPercent { get; set; }
        public DateTime? LastReadingTime { get; set; }

        public string WasteType { get; set; } = "";
        public int CapacityLiters { get; set; }
    }
}
