using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartWaste.Web.Models;
using System.Data;

namespace SmartWaste.Web.Areas.Operator.Controllers
{
    [Area("Operator")]
    [Authorize(Roles = "Operator")]
    public class ReadingsController : Controller
    {
        private readonly IConfiguration _config;
        public ReadingsController(IConfiguration config) => _config = config;

        [HttpGet]
        public async Task<IActionResult> Add(int? binId)
        {
            var vm = new AddReadingVm
            {
                BinId = binId ?? 0,
                FillLevelPercent = 0
            };

            await LoadBins(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddReadingVm vm)
        {
            if (!ModelState.IsValid)
            {
                await LoadBins(vm);
                return View(vm);
            }

            var cs = _config.GetConnectionString("DefaultConnection");

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_AddBinReading", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@BinId", vm.BinId);
            cmd.Parameters.AddWithValue("@FillLevelPercent", vm.FillLevelPercent);
            cmd.Parameters.AddWithValue("@Temperature", (object?)vm.Temperature ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReadingTime", (object?)vm.ReadingTime ?? DBNull.Value);

            await cmd.ExecuteScalarAsync();

            TempData["Msg"] = "Reading added successfully (bin status updated).";
            return RedirectToAction("Index", "Bins");
        }

        private async Task LoadBins(AddReadingVm vm)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var list = new List<(string Value, string Text)>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            var sql = "SELECT BinId, Location FROM dbo.Bins ORDER BY BinId DESC";
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add((r["BinId"].ToString()!, $"{r["BinId"]} - {r["Location"]}"));
            }

            vm.Bins = list;
        }
    }
}
