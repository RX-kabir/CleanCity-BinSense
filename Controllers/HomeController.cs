using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartWaste.Web.Models;

namespace SmartWaste.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // If user already logged in, send them to their dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return Redirect("/Admin/Dashboard");
                if (User.IsInRole("Operator")) return Redirect("/Operator/Dashboard");
                if (User.IsInRole("Driver")) return Redirect("/Driver/Dashboard");
            }

            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult AppError()
        {
            return View("~/Views/Shared/AppError.cshtml");
        }

        [HttpGet]
        public IActionResult StatusCode(int code)
        {
            if (code == 404)
                return View("~/Views/Shared/NotFound.cshtml");

            // fallback for other status codes
            return View("~/Views/Shared/AppError.cshtml");
        }

    }
}
