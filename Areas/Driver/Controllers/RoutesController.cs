using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartWaste.Web.Models;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace SmartWaste.Web.Areas.Driver.Controllers
{
    [Area("Driver")]
    [Authorize(Roles = "Driver")]
    public class RoutesController : Controller
    {
        private readonly IConfiguration _config;
        public RoutesController(IConfiguration config) => _config = config;

        // My routes
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "My Routes";
            var cs = _config.GetConnectionString("DefaultConnection");
            var list = new List<DriverRouteRowVm>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_DriverMyRoutes", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DriverUserEmail", User.Identity?.Name ?? "");

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new DriverRouteRowVm
                {
                    RouteId = Convert.ToInt32(r["RouteId"]),
                    RouteDate = Convert.ToDateTime(r["RouteDate"]),
                    RouteStatusId = Convert.ToByte(r["RouteStatusId"]),
                    RouteStatus = Convert.ToString(r["RouteStatus"]) ?? "",
                    RegistrationNo = Convert.ToString(r["RegistrationNo"]) ?? "",
                    DriverName = Convert.ToString(r["DriverName"]) ?? "",
                    TotalStops = Convert.ToInt32(r["TotalStops"]),
                    CompletedStops = Convert.ToInt32(r["CompletedStops"])
                });
            }

            return View(list);
        }

        // Route map + stops table for driver
        public async Task<IActionResult> Map(int id)
        {
            ViewData["Title"] = $"Route #{id}";
            var cs = _config.GetConnectionString("DefaultConnection");
            var vm = new DriverRouteDetailsVm();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            // header
            await using (var cmd = new SqlCommand("dbo.sp_DriverRouteHeader", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RouteId", id);

                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    vm.Header = new DriverRouteRowVm
                    {
                        RouteId = Convert.ToInt32(r["RouteId"]),
                        RouteDate = Convert.ToDateTime(r["RouteDate"]),
                        RouteStatusId = Convert.ToByte(r["RouteStatusId"]),
                        RouteStatus = Convert.ToString(r["RouteStatus"]) ?? "",
                        RegistrationNo = Convert.ToString(r["RegistrationNo"]) ?? "",
                        DriverName = Convert.ToString(r["DriverName"]) ?? "",
                        TotalStops = Convert.ToInt32(r["TotalStops"]),
                        CompletedStops = Convert.ToInt32(r["CompletedStops"])
                    };
                }
            }

            // stops via your existing SP used by Admin map
            await using (var cmd = new SqlCommand("dbo.sp_GetRouteMapData", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RouteId", id);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    vm.Stops.Add(new RouteStopMapDto
                    {
                        RouteStopId = reader.GetInt64(reader.GetOrdinal("RouteStopId")),
                        RouteId = reader.GetInt32(reader.GetOrdinal("RouteId")),
                        StopOrder = reader.GetInt32(reader.GetOrdinal("StopOrder")),
                        PlannedTime = reader.IsDBNull(reader.GetOrdinal("PlannedTime")) ? null : reader.GetDateTime(reader.GetOrdinal("PlannedTime")),
                        ActualTime = reader.IsDBNull(reader.GetOrdinal("ActualTime")) ? null : reader.GetDateTime(reader.GetOrdinal("ActualTime")),
                        CollectedVolumeLiters = reader.IsDBNull(reader.GetOrdinal("CollectedVolumeLiters")) ? null : reader.GetDecimal(reader.GetOrdinal("CollectedVolumeLiters")),
                        BinId = reader.GetInt32(reader.GetOrdinal("BinId")),
                        Location = reader.GetString(reader.GetOrdinal("Location")),
                        Latitude = reader.GetDecimal(reader.GetOrdinal("Latitude")),
                        Longitude = reader.GetDecimal(reader.GetOrdinal("Longitude")),
                        BinStatusId = reader.GetByte(reader.GetOrdinal("BinStatusId")),
                        StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                        CapacityLiters = reader.GetInt32(reader.GetOrdinal("CapacityLiters")),
                        WasteType = reader.GetString(reader.GetOrdinal("WasteType"))
                    });
                }
            }

            return View(vm);
        }

        // JSON: real-world route line for map
        [HttpGet]
        public async Task<IActionResult> RouteLine(int id)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var latLngs = await GetRouteLineLatLngs(cs, id);
            return Json(latLngs);
        }

        // GET: Log pickup form
        [HttpGet]
        public IActionResult LogPickup(long routeStopId, int routeId, string? binLocation)
        {
            ViewData["Title"] = "Log Pickup";
            return View(new DriverLogPickupVm
            {
                RouteStopId = routeStopId,
                RouteId = routeId,
                BinLocation = binLocation ?? ""
            });
        }

        // POST: Log pickup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogPickup(DriverLogPickupVm vm)
        {
            if (vm.VolumeCollectedLiters <= 0)
            {
                ModelState.AddModelError("", "Volume must be greater than 0.");
                return View(vm);
            }

            var cs = _config.GetConnectionString("DefaultConnection");

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_DriverLogPickup", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RouteStopId", vm.RouteStopId);
            cmd.Parameters.AddWithValue("@VolumeCollectedLiters", vm.VolumeCollectedLiters);

            await cmd.ExecuteScalarAsync();

            TempData["Msg"] = "Pickup logged successfully.";
            return RedirectToAction("Map", new { id = vm.RouteId });
        }

        private async Task<List<double[]>> GetRouteLineLatLngs(string cs, int routeId)
        {
            var stops = new List<RouteStopMapDto>();
            await using (var conn = new SqlConnection(cs))
            {
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("dbo.sp_GetRouteMapData", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RouteId", routeId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    stops.Add(new RouteStopMapDto
                    {
                        RouteStopId = reader.GetInt64(reader.GetOrdinal("RouteStopId")),
                        RouteId = reader.GetInt32(reader.GetOrdinal("RouteId")),
                        StopOrder = reader.GetInt32(reader.GetOrdinal("StopOrder")),
                        PlannedTime = reader.IsDBNull(reader.GetOrdinal("PlannedTime")) ? null : reader.GetDateTime(reader.GetOrdinal("PlannedTime")),
                        ActualTime = reader.IsDBNull(reader.GetOrdinal("ActualTime")) ? null : reader.GetDateTime(reader.GetOrdinal("ActualTime")),
                        CollectedVolumeLiters = reader.IsDBNull(reader.GetOrdinal("CollectedVolumeLiters")) ? null : reader.GetDecimal(reader.GetOrdinal("CollectedVolumeLiters")),
                        BinId = reader.GetInt32(reader.GetOrdinal("BinId")),
                        Location = reader.GetString(reader.GetOrdinal("Location")),
                        Latitude = reader.GetDecimal(reader.GetOrdinal("Latitude")),
                        Longitude = reader.GetDecimal(reader.GetOrdinal("Longitude")),
                        BinStatusId = reader.GetByte(reader.GetOrdinal("BinStatusId")),
                        StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                        CapacityLiters = reader.GetInt32(reader.GetOrdinal("CapacityLiters")),
                        WasteType = reader.GetString(reader.GetOrdinal("WasteType"))
                    });
                }
            }

            var orderedStops = stops.OrderBy(s => s.StopOrder).ToList();
            if (orderedStops.Count < 2)
                return new List<double[]>();

            var coordPairs = orderedStops
                .Select(s => $"{s.Longitude.ToString(CultureInfo.InvariantCulture)},{s.Latitude.ToString(CultureInfo.InvariantCulture)}")
                .ToArray();

            var url = $"https://router.project-osrm.org/route/v1/driving/{string.Join(";", coordPairs)}?overview=full&geometries=geojson";

            using var http = new HttpClient();
            using var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<double[]>();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (!doc.RootElement.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                return new List<double[]>();

            var geometry = routes[0].GetProperty("geometry");
            if (!geometry.TryGetProperty("coordinates", out var coords))
                return new List<double[]>();

            var latLngs = new List<double[]>();
            foreach (var coord in coords.EnumerateArray())
            {
                if (coord.GetArrayLength() < 2) continue;
                var lng = coord[0].GetDouble();
                var lat = coord[1].GetDouble();
                latLngs.Add(new[] { lat, lng });
            }

            return latLngs;
        }
    }
}
