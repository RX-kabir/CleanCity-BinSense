using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartWaste.Web.Models;
using System.Data;

namespace SmartWaste.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IConfiguration _config;

        public DashboardController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Admin Dashboard";

            var cs = _config.GetConnectionString("DefaultConnection");
            var vm = new AdminDashboardVm();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            // Summary
            await using (var cmd = new SqlCommand("dbo.sp_AdminDashboardSummary", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                await using var r = await cmd.ExecuteReaderAsync();

                if (await r.ReadAsync())
                {
                    vm.TotalBins = Convert.ToInt32(r["TotalBins"]);
                    vm.OkBins = Convert.ToInt32(r["OkBins"]);
                    vm.NeedsPickupBins = Convert.ToInt32(r["NeedsPickupBins"]);
                    vm.OverflowingBins = Convert.ToInt32(r["OverflowingBins"]);

                    vm.PickupsToday = Convert.ToInt32(r["PickupsToday"]);
                    vm.VolumeTodayLiters = Convert.ToDecimal(r["VolumeTodayLiters"]);

                    vm.RoutesToday = Convert.ToInt32(r["RoutesToday"]);
                    vm.PlannedRoutes = Convert.ToInt32(r["PlannedRoutes"]);
                    vm.InProgressRoutes = Convert.ToInt32(r["InProgressRoutes"]);
                    vm.CompletedRoutes = Convert.ToInt32(r["CompletedRoutes"]);
                    vm.CancelledRoutes = Convert.ToInt32(r["CancelledRoutes"]);

                }
            }

            // Trend
            await using (var cmd = new SqlCommand("dbo.sp_AdminPickupsTrend", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Days", 7);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    vm.Trend.Add(new PickupTrendPoint
                    {
                        Day = Convert.ToDateTime(r["Day"]),
                        PickupsCount = Convert.ToInt32(r["PickupsCount"]),
                        VolumeLiters = Convert.ToDecimal(r["VolumeLiters"])
                    });
                }
            }

            // Recent
            await using (var cmd = new SqlCommand("dbo.sp_AdminRecentPickups", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Top", 8);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    vm.RecentPickups.Add(new RecentPickupRow
                    {
                        PickupId = Convert.ToInt64(r["PickupId"]),
                        PickupTime = Convert.ToDateTime(r["PickupTime"]),
                        VolumeCollectedLiters = Convert.ToDecimal(r["VolumeCollectedLiters"]),
                        BinId = Convert.ToInt32(r["BinId"]),
                        Location = Convert.ToString(r["Location"]) ?? "",
                        RegistrationNo = Convert.ToString(r["RegistrationNo"]) ?? "",
                        DriverName = Convert.ToString(r["DriverName"]) ?? ""
                    });
                }
            }

            return View(vm);
        }
    }
}
