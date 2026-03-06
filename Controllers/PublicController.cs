using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartWaste.Web.Models;

namespace SmartWaste.Web.Controllers
{
    public class PublicController : Controller
    {
        private readonly IConfiguration _config;

        public PublicController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("/public/map")]
        public IActionResult Map() => Redirect("/PublicMap");


        // JSON endpoint for Leaflet map
        [HttpGet("/public/nearby")]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Nearby(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] int radiusMeters = 1000,
            [FromQuery] byte? statusId = null)
        {
            if (radiusMeters < 50) radiusMeters = 50;
            if (radiusMeters > 5000) radiusMeters = 5000;

            var cs = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(cs))
                return Problem("DefaultConnection is missing.");

            var results = new List<NearbyBinDto>();

            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.sp_GetNearbyBins", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Latitude", lat);
            cmd.Parameters.AddWithValue("@Longitude", lng);
            cmd.Parameters.AddWithValue("@RadiusMeters", radiusMeters);
            cmd.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new NearbyBinDto
                {
                    BinId = reader.GetInt32(reader.GetOrdinal("BinId")),
                    Location = reader.GetString(reader.GetOrdinal("Location")),
                    Latitude = reader.GetDecimal(reader.GetOrdinal("Latitude")),
                    Longitude = reader.GetDecimal(reader.GetOrdinal("Longitude")),
                    CapacityLiters = reader.GetInt32(reader.GetOrdinal("CapacityLiters")),
                    WasteType = reader.GetString(reader.GetOrdinal("WasteType")),
                    BinStatusId = reader.GetByte(reader.GetOrdinal("BinStatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
                    LatestFillLevelPercent = reader.GetInt32(reader.GetOrdinal("LatestFillLevelPercent")),
                    LastReadingTime = reader.IsDBNull(reader.GetOrdinal("LastReadingTime"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("LastReadingTime")),
                    DistanceMeters = reader.GetInt32(reader.GetOrdinal("DistanceMeters"))
                });
            }

            return Json(results);
        }
    }
}
