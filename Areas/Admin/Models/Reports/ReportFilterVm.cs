namespace SmartWaste.Web.Areas.Admin.Models.Reports
{
    public class ReportFilterVm
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public int? ZoneId { get; set; }
        public int? TruckId { get; set; }

        // dropdown data
        public List<(int Id, string Name)> Zones { get; set; } = new();
        public List<(int Id, string Name)> Trucks { get; set; } = new();
    }
}
