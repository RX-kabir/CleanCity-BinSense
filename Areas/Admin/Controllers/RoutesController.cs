using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartWaste.Web.Models;
using System.Data;
using System.Globalization;
using System.Text.Json;


namespace SmartWaste.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoutesController : Controller
    {
        private readonly IConfiguration _config;

        public RoutesController(IConfiguration config)
        {
            _config = config;
        }

        // 1) List routes
        public async Task<IActionResult> Index()
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var routes = new List<dynamic>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            var sql = @"
SELECT TOP 100
    r.RouteId, r.RouteDate, r.RouteStatusId, rs.Name AS RouteStatus,
    t.RegistrationNo,
    u.FullName AS DriverName,
    (SELECT COUNT(*) FROM dbo.RouteStops s WHERE s.RouteId = r.RouteId) AS TotalStops
FROM dbo.Routes r
JOIN dbo.RouteStatus rs ON rs.RouteStatusId = r.RouteStatusId
JOIN dbo.Trucks t ON t.TruckId = r.TruckId
JOIN dbo.Drivers d ON d.DriverId = r.DriverId
JOIN dbo.Users u ON u.UserId = d.UserId
ORDER BY r.RouteId DESC;";

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                routes.Add(new
                {
                    RouteId = reader.GetInt32(0),
                    RouteDate = reader.GetDateTime(1),
                    RouteStatusId = reader.GetByte(2),
                    RouteStatus = reader.GetString(3),
                    RegistrationNo = reader.GetString(4),
                    DriverName = reader.GetString(5),
                    TotalStops = reader.GetInt32(6)
                });
            }

            return View(routes);
        }

        // 2) Plan route form
        [HttpGet]
        public async Task<IActionResult> Plan()
        {
            var cs = _config.GetConnectionString("DefaultConnection");

            ViewBag.Zones = await LoadDropDown(cs, "SELECT ZoneId, Name FROM dbo.Zones ORDER BY Name", "ZoneId", "Name");
            ViewBag.Trucks = await LoadDropDown(cs, "SELECT TruckId, RegistrationNo FROM dbo.Trucks ORDER BY RegistrationNo", "TruckId", "RegistrationNo");
            ViewBag.Drivers = await LoadDropDown(cs, "SELECT d.DriverId, u.FullName FROM dbo.Drivers d JOIN dbo.Users u ON u.UserId=d.UserId ORDER BY u.FullName", "DriverId", "FullName");

            ViewBag.Today = DateTime.UtcNow.Date;
            return View();
        }

        // 3) Plan route submit -> calls sp_PlanRouteForZone -> redirect to Map
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Plan(int zoneId, DateTime routeDate, int truckId, int driverId, int minFillPercent = 70)
        {
            var cs = _config.GetConnectionString("DefaultConnection");

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_PlanRouteForZone", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ZoneId", zoneId);
            cmd.Parameters.AddWithValue("@RouteDate", routeDate.Date);
            cmd.Parameters.AddWithValue("@TruckId", truckId);
            cmd.Parameters.AddWithValue("@DriverId", driverId);
            cmd.Parameters.AddWithValue("@MinFillPercent", minFillPercent);

            var routeIdObj = await cmd.ExecuteScalarAsync();
            var routeId = Convert.ToInt32(routeIdObj);

            return RedirectToAction("Map", new { id = routeId });
        }

        // 4) Route map page (NOW loads header info)
        public async Task<IActionResult> Map(int id)
        {
            var cs = _config.GetConnectionString("DefaultConnection");

            var header = await LoadRouteHeader(cs, id);
            if (header == null) return NotFound();

            var stops = await LoadRouteStops(cs, id);

            var vm = new RouteMapPageViewModel
            {
                RouteId = header.RouteId,
                RouteDate = header.RouteDate,
                RouteStatusId = header.RouteStatusId,
                RouteStatus = header.RouteStatus,
                StartedAt = header.StartedAt,
                CompletedAt = header.CompletedAt,
                TruckId = header.TruckId,
                RegistrationNo = header.RegistrationNo,
                DriverId = header.DriverId,
                DriverName = header.DriverName,
                TotalStops = header.TotalStops,
                CompletedStops = header.CompletedStops,
                Stops = stops
            };

            return View(vm);
        }


        // 5) JSON: route stops for map
        [HttpGet]
        public async Task<IActionResult> Stops(int id)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var results = new List<RouteStopMapDto>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_GetRouteMapData", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RouteId", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new RouteStopMapDto
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

            return Json(results);
        }

        // 5b) JSON: real-world route line for map
        [HttpGet]
        public async Task<IActionResult> RouteLine(int id)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var latLngs = await GetRouteLineLatLngs(cs, id);
            return Json(latLngs);
        }

        // Start route
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            var cs = _config.GetConnectionString("DefaultConnection");

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_StartRoute", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RouteId", id);

            await cmd.ExecuteNonQueryAsync();
            return RedirectToAction("Map", new { id });
        }

        // Complete route
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var cs = _config.GetConnectionString("DefaultConnection");

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_CompleteRoute", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RouteId", id);

            await cmd.ExecuteNonQueryAsync();
            return RedirectToAction("Map", new { id });
        }

        // helper for dropdown lists
        private static async Task<List<(string Value, string Text)>> LoadDropDown(string cs, string sql, string valueCol, string textCol)
        {
            var list = new List<(string Value, string Text)>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var value = reader[valueCol]?.ToString() ?? "";
                var text = reader[textCol]?.ToString() ?? "";
                list.Add((value, text));
            }

            return list;
        }

        // NEW: loads route header info for Map page
        private static async Task<RouteMapViewModel?> LoadRouteHeader(string cs, int routeId)
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_GetRouteHeader", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RouteId", routeId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new RouteMapViewModel
            {
                RouteId = reader.GetInt32(reader.GetOrdinal("RouteId")),
                RouteDate = reader.GetDateTime(reader.GetOrdinal("RouteDate")),
                RouteStatusId = reader.GetByte(reader.GetOrdinal("RouteStatusId")),
                RouteStatus = reader.GetString(reader.GetOrdinal("RouteStatus")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                TruckId = reader.GetInt32(reader.GetOrdinal("TruckId")),
                RegistrationNo = reader.GetString(reader.GetOrdinal("RegistrationNo")),
                DriverId = reader.GetInt32(reader.GetOrdinal("DriverId")),
                DriverName = reader.GetString(reader.GetOrdinal("DriverName")),
                TotalStops = reader.GetInt32(reader.GetOrdinal("TotalStops")),
                CompletedStops = reader.GetInt32(reader.GetOrdinal("CompletedStops"))
            };
        }

        private static async Task<List<RouteStopMapDto>> LoadRouteStops(string cs, int routeId)
        {
            var results = new List<RouteStopMapDto>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_GetRouteMapData", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RouteId", routeId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new RouteStopMapDto
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

            return results;
        }

        private async Task<List<double[]>> GetRouteLineLatLngs(string cs, int routeId)
        {
            var stops = await LoadRouteStops(cs, routeId);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogPickup(long routeStopId, int routeId, decimal volumeCollectedLiters)
        {
            var cs = _config.GetConnectionString("DefaultConnection");

            try
            {
                await using var conn = new SqlConnection(cs);
                await conn.OpenAsync();

                await using var cmd = new SqlCommand("dbo.sp_LogPickupForRouteStop", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RouteStopId", routeStopId);
                cmd.Parameters.AddWithValue("@VolumeCollectedLiters", volumeCollectedLiters);

                await cmd.ExecuteNonQueryAsync();
                TempData["Msg"] = "Pickup logged successfully.";
            }
            catch (SqlException ex)
            {
                TempData["Err"] = ex.Message;
            }

            return RedirectToAction("Map", new { id = routeId });
        }

        [HttpGet]
        public async Task<IActionResult> ZoneBins(int id)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var results = new List<BinMapDto>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_GetBinsForRouteZone", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@RouteId", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new BinMapDto
                {
                    BinId = reader.GetInt32(reader.GetOrdinal("BinId")),
                    Location = reader.GetString(reader.GetOrdinal("Location")),
                    Latitude = reader.GetDecimal(reader.GetOrdinal("Latitude")),
                    Longitude = reader.GetDecimal(reader.GetOrdinal("Longitude")),
                    BinStatusId = reader.GetByte(reader.GetOrdinal("BinStatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                    LatestFillLevelPercent = reader.GetInt32(reader.GetOrdinal("LatestFillLevelPercent")),
                    LastReadingTime = reader.IsDBNull(reader.GetOrdinal("LastReadingTime")) ? null : reader.GetDateTime(reader.GetOrdinal("LastReadingTime")),
                    WasteType = reader.GetString(reader.GetOrdinal("WasteType")),
                    CapacityLiters = reader.GetInt32(reader.GetOrdinal("CapacityLiters"))
                });
            }

            return Json(results);
        }





    }
}
