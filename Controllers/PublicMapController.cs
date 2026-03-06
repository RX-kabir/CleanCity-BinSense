using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartWaste.Web.Models;
using System.Data;

namespace SmartWaste.Web.Controllers
{
    public class PublicMapController : Controller
    {
        private readonly IConfiguration _config;
        public PublicMapController(IConfiguration config) => _config = config;

        // Page
        [HttpGet]
        public IActionResult Index() => View();

        // API: /api/public/bins?lat=..&lng=..&radiusKm=2
        [HttpGet("/api/public/bins")]
        public async Task<IActionResult> Bins(decimal lat, decimal lng, double radiusKm = 2.0, int maxResults = 200)
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            var results = new List<PublicBinDto>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_Public_GetBinsNear", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Latitude", lat);
            cmd.Parameters.AddWithValue("@Longitude", lng);
            cmd.Parameters.AddWithValue("@RadiusKm", radiusKm);
            cmd.Parameters.AddWithValue("@MaxResults", maxResults);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new PublicBinDto
                {
                    BinId = reader.GetInt32(reader.GetOrdinal("BinId")),
                    Location = reader.GetString(reader.GetOrdinal("Location")),
                    Latitude = reader.GetDecimal(reader.GetOrdinal("Latitude")),
                    Longitude = reader.GetDecimal(reader.GetOrdinal("Longitude")),
                    WasteType = reader.GetString(reader.GetOrdinal("WasteType")),
                    CapacityLiters = reader.GetInt32(reader.GetOrdinal("CapacityLiters")),
                    BinStatusId = reader.GetByte(reader.GetOrdinal("BinStatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                    LatestFillLevelPercent = reader.GetInt32(reader.GetOrdinal("LatestFillLevelPercent")),
                    LastReadingTime = reader.IsDBNull(reader.GetOrdinal("LastReadingTime")) ? null : reader.GetDateTime(reader.GetOrdinal("LastReadingTime")),
                    DistanceKm = reader.GetDecimal(reader.GetOrdinal("DistanceKm"))
                });
            }

            return Json(results);
        }
    }
}
