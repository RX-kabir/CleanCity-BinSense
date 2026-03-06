using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartWaste.Web.Areas.Admin.Models.Reports;
using System.Data;
using System.Text;

namespace SmartWaste.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly IConfiguration _config;
        public ReportsController(IConfiguration config) => _config = config;

        // Reports Home
        public IActionResult Index() => View();

        // -------------------------
        // Zone Waste Summary
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> ZoneSummary(DateTime? from, DateTime? to, int? zoneId)
        {
            var vm = new ReportFilterVm
            {
                From = from ?? DateTime.Today.AddDays(-7),
                To = to ?? DateTime.Today,
                ZoneId = zoneId
            };

            await LoadZones(vm);

            var rows = await GetZoneWasteRows(vm.From!.Value, vm.To!.Value, vm.ZoneId);
            ViewBag.Rows = rows;

            return View(vm);
        }

        // CSV Export
        [HttpGet]
        public async Task<IActionResult> ZoneSummaryCsv(DateTime? from, DateTime? to, int? zoneId)
        {
            var f = from ?? DateTime.Today.AddDays(-7);
            var t = to ?? DateTime.Today;
            var rows = await GetZoneWasteRows(f, t, zoneId);

            var sb = new StringBuilder();
            sb.AppendLine("Zone,Date,TotalCollectedLiters,PickupCount");
            foreach (var r in rows)
                sb.AppendLine($"{Csv(r.ZoneName)},{r.ReportDate:yyyy-MM-dd},{r.TotalCollectedLiters},{r.PickupCount}");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "ZoneWasteSummary.csv");
        }

        // -------------------------
        // Truck Route Summary
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> TruckSummary(DateTime? from, DateTime? to, int? truckId)
        {
            var vm = new ReportFilterVm
            {
                From = from ?? DateTime.Today.AddDays(-7),
                To = to ?? DateTime.Today,
                TruckId = truckId
            };

            await LoadTrucks(vm);

            var rows = await GetTruckRouteRows(vm.From!.Value, vm.To!.Value, vm.TruckId);
            ViewBag.Rows = rows;

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> TruckSummaryCsv(DateTime? from, DateTime? to, int? truckId)
        {
            var f = from ?? DateTime.Today.AddDays(-7);
            var t = to ?? DateTime.Today;
            var rows = await GetTruckRouteRows(f, t, truckId);

            var sb = new StringBuilder();
            sb.AppendLine("Truck,Date,Status,TotalStops,CompletedStops");
            foreach (var r in rows)
                sb.AppendLine($"{Csv(r.TruckNo)},{r.RouteDate:yyyy-MM-dd},{Csv(r.RouteStatus)},{r.TotalStops},{r.CompletedStops}");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "TruckRouteSummary.csv");
        }

        // -------------------------
        // Analytics Page
        // -------------------------
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> Analytics(DateTime? from, DateTime? to)
        {
            var vm = new AnalyticsVm
            {
                From = (from ?? DateTime.Today.AddDays(-7)).Date,
                To = (to ?? DateTime.Today).Date
            };

            var cs = _config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_Admin_AnalyticsSnapshot", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FromDate", vm.From);
            cmd.Parameters.AddWithValue("@ToDate", vm.To);

            await using var rd = await cmd.ExecuteReaderAsync();

            // Result set 1: KPIs
            if (await rd.ReadAsync())
            {
                vm.TotalPickups = rd.GetInt32(rd.GetOrdinal("TotalPickups"));
                vm.TotalCollectedLiters = rd.GetDecimal(rd.GetOrdinal("TotalCollectedLiters"));
                vm.OverflowAlerts = rd.GetInt32(rd.GetOrdinal("OverflowAlerts"));
                vm.TotalRoutes = rd.GetInt32(rd.GetOrdinal("TotalRoutes"));
            }

            // Result set 2: Trend
            await rd.NextResultAsync();
            while (await rd.ReadAsync())
            {
                vm.Trend.Add(new DailyTrendRow
                {
                    Day = rd.GetDateTime(rd.GetOrdinal("Day")),
                    PickupCount = rd.GetInt32(rd.GetOrdinal("PickupCount")),
                    CollectedLiters = rd.GetDecimal(rd.GetOrdinal("CollectedLiters"))
                });
            }

            // Result set 3: Top Zones
            await rd.NextResultAsync();
            while (await rd.ReadAsync())
            {
                vm.TopZones.Add(new TopZoneRow
                {
                    ZoneName = rd.GetString(rd.GetOrdinal("ZoneName")),
                    TotalCollectedLiters = rd.GetDecimal(rd.GetOrdinal("TotalCollectedLiters")),
                    PickupCount = rd.GetInt32(rd.GetOrdinal("PickupCount"))
                });
            }

            // Result set 4: Top Overflow Bins
            await rd.NextResultAsync();
            while (await rd.ReadAsync())
            {
                vm.TopOverflowBins.Add(new TopOverflowBinRow
                {
                    BinId = rd.GetInt32(rd.GetOrdinal("BinId")),
                    Location = rd.GetString(rd.GetOrdinal("Location")),
                    ZoneName = rd.GetString(rd.GetOrdinal("ZoneName")),
                    OverflowCount = rd.GetInt32(rd.GetOrdinal("OverflowCount"))
                });
            }

            // Result set 5: Truck Utilization
            await rd.NextResultAsync();
            while (await rd.ReadAsync())
            {
                vm.TruckUtilization.Add(new TruckUtilRow
                {
                    RegistrationNo = rd.GetString(rd.GetOrdinal("RegistrationNo")),
                    Routes = rd.GetInt32(rd.GetOrdinal("Routes")),
                    TotalStops = rd.GetInt32(rd.GetOrdinal("TotalStops")),
                    CompletedStops = rd.GetInt32(rd.GetOrdinal("CompletedStops"))
                });
            }

            return View(vm);
        }

        // -------------------------
        // Helpers (DB)
        // -------------------------
        private async Task LoadZones(ReportFilterVm vm)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var list = new List<(int Id, string Name)>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT ZoneId, Name FROM dbo.Zones ORDER BY Name", conn);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                list.Add((rd.GetInt32(0), rd.GetString(1)));

            vm.Zones = list;
        }

        private async Task LoadTrucks(ReportFilterVm vm)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var list = new List<(int Id, string Name)>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("SELECT TruckId, RegistrationNo FROM dbo.Trucks ORDER BY RegistrationNo", conn);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
                list.Add((rd.GetInt32(0), rd.GetString(1)));

            vm.Trucks = list;
        }

        private async Task<List<ZoneWasteRow>> GetZoneWasteRows(DateTime from, DateTime to, int? zoneId)
        {
            // Uses your PickupEvents + Bins + Zones
            // If you already have a view like vw_ZoneWasteSummary, we can switch to it later.
            var cs = _config.GetConnectionString("DefaultConnection");
            var rows = new List<ZoneWasteRow>();

            var sql = @"
SELECT
    z.Name AS ZoneName,
    CAST(p.PickupTime as date) as ReportDate,
    SUM(p.VolumeCollectedLiters) as TotalCollectedLiters,
    COUNT(*) as PickupCount
FROM dbo.PickupEvents p
JOIN dbo.Bins b ON b.BinId = p.BinId
JOIN dbo.Zones z ON z.ZoneId = b.ZoneId
WHERE p.PickupTime >= @FromDate
  AND p.PickupTime < DATEADD(day, 1, @ToDate)
  AND (@ZoneId IS NULL OR z.ZoneId = @ZoneId)
GROUP BY z.Name, CAST(p.PickupTime as date)
ORDER BY ReportDate DESC, ZoneName;";

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FromDate", from.Date);
            cmd.Parameters.AddWithValue("@ToDate", to.Date);
            cmd.Parameters.AddWithValue("@ZoneId", (object?)zoneId ?? DBNull.Value);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                rows.Add(new ZoneWasteRow
                {
                    ZoneName = rd.GetString(0),
                    ReportDate = rd.GetDateTime(1),
                    TotalCollectedLiters = rd.IsDBNull(2) ? 0 : rd.GetDecimal(2),
                    PickupCount = rd.GetInt32(3)
                });
            }

            return rows;
        }

        private async Task<List<TruckRouteRow>> GetTruckRouteRows(DateTime from, DateTime to, int? truckId)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var rows = new List<TruckRouteRow>();

            var sql = @"
SELECT
    t.RegistrationNo AS TruckNo,
    r.RouteDate,
    rs.Name AS RouteStatus,
    (SELECT COUNT(*) FROM dbo.RouteStops s WHERE s.RouteId = r.RouteId) AS TotalStops,
    (SELECT COUNT(*) FROM dbo.RouteStops s WHERE s.RouteId = r.RouteId AND s.ActualTime IS NOT NULL) AS CompletedStops
FROM dbo.Routes r
JOIN dbo.Trucks t ON t.TruckId = r.TruckId
JOIN dbo.RouteStatus rs ON rs.RouteStatusId = r.RouteStatusId
WHERE r.RouteDate >= @FromDate
  AND r.RouteDate <= @ToDate
  AND (@TruckId IS NULL OR t.TruckId = @TruckId)
ORDER BY r.RouteDate DESC, TruckNo;";

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FromDate", from.Date);
            cmd.Parameters.AddWithValue("@ToDate", to.Date);
            cmd.Parameters.AddWithValue("@TruckId", (object?)truckId ?? DBNull.Value);

            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                rows.Add(new TruckRouteRow
                {
                    TruckNo = rd.GetString(0),
                    RouteDate = rd.GetDateTime(1),
                    RouteStatus = rd.GetString(2),
                    TotalStops = rd.GetInt32(3),
                    CompletedStops = rd.GetInt32(4)
                });
            }

            return rows;
        }

        private static string Csv(string s)
        {
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
