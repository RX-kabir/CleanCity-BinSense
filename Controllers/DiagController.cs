using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartWaste.Web.Controllers
{
    [AllowAnonymous]
    public class DiagController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public DiagController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Usage:
        // /diag/find?needle=Use%20My%20Location
        [HttpGet("/diag/find")]
        public IActionResult Find(string needle)
        {
            if (string.IsNullOrWhiteSpace(needle))
                return BadRequest("Provide ?needle=...");

            var root = _env.ContentRootPath;

            // Only scan common UI folders (fast)
            var folders = new[]
            {
                Path.Combine(root, "Views"),
                Path.Combine(root, "Areas"),
                Path.Combine(root, "Pages"),
                Path.Combine(root, "wwwroot")
            };

            var matches = new List<string>();
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;

                var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                                     .Where(f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ||
                                                 f.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                                                 f.EndsWith(".html", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    var text = System.IO.File.ReadAllText(file);
                    if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                        matches.Add(file.Replace(root, "").Replace("\\", "/"));
                }
            }

            return Json(new { needle, matches });
        }
    }
}
