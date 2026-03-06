namespace SmartWaste.Web.Models
{
    public class OperatorDashboardVm
    {
        public int TotalBins { get; set; }
        public int OkBins { get; set; }
        public int NeedsPickupBins { get; set; }
        public int OverflowingBins { get; set; }
        public int ReadingsToday { get; set; }

        public List<BinListRowVm> AttentionBins { get; set; } = new();
    }
}
