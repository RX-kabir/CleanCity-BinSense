namespace SmartWaste.Web.Models
{
    public class RouteMapPageViewModel : RouteMapViewModel
    {
        public List<RouteStopMapDto> Stops { get; set; } = new();
    }
}
