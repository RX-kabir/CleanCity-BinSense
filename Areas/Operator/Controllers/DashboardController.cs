using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartWaste.Web.Models;
using System.Data;

namespace SmartWaste.Web.Areas.Operator.Controllers
{
    [Area("Operator")]
    [Authorize(Roles = "Operator")]
    public class DashboardController : Controller
    {
        private readonly IConfiguration _config;
        public DashboardController(IConfiguration config) => _config = config;

        public async Task<IActionResult> Index()
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var vm = new OperatorDashboardVm();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            // Summary
            await using (var cmd = new SqlCommand("dbo.sp_OperatorDashboardSummary", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    vm.TotalBins = Convert.ToInt32(r["TotalBins"]);
                    vm.OkBins = Convert.ToInt32(r["OkBins"]);
                    vm.NeedsPickupBins = Convert.ToInt32(r["NeedsPickupBins"]);
                    vm.OverflowingBins = Convert.ToInt32(r["OverflowingBins"]);
                    vm.ReadingsToday = Convert.ToInt32(r["ReadingsToday"]);
                }
            }

            // Attention bins (NeedsPickup/Overflowing) using list SP with Status filter = null, we filter in C#
            await using (var cmd = new SqlCommand("dbo.sp_ListBinsWithLatest", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ZoneId", DBNull.Value);
                cmd.Parameters.AddWithValue("@StatusId", DBNull.Value);
                cmd.Parameters.AddWithValue("@Search", DBNull.Value);

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var statusName = Convert.ToString(r["StatusName"]) ?? "";
                    if (statusName.Equals("OK", StringComparison.OrdinalIgnoreCase))
                        continue;

                    vm.AttentionBins.Add(new BinListRowVm
                    {
                        BinId = Convert.ToInt32(r["BinId"]),
                        ZoneId = Convert.ToInt32(r["ZoneId"]),
                        ZoneName = Convert.ToString(r["ZoneName"]) ?? "",
                        Location = Convert.ToString(r["Location"]) ?? "",
                        Latitude = Convert.ToDecimal(r["Latitude"]),
                        Longitude = Convert.ToDecimal(r["Longitude"]),
                        CapacityLiters = Convert.ToInt32(r["CapacityLiters"]),
                        WasteType = Convert.ToString(r["WasteType"]) ?? "",
                        BinStatusId = Convert.ToByte(r["BinStatusId"]),
                        StatusName = statusName,
                        LatestFillLevelPercent = r["LatestFillLevelPercent"] == DBNull.Value ? null : Convert.ToInt32(r["LatestFillLevelPercent"]),
                        LastReadingTime = r["LastReadingTime"] == DBNull.Value ? null : Convert.ToDateTime(r["LastReadingTime"]),
                    });

                    if (vm.AttentionBins.Count >= 8) break;
                }
            }

            return View(vm);
        }
    }
}
