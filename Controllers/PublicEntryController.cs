using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace SmartWaste.Web.Controllers
{
    [AllowAnonymous]
    public class PublicEntryController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public PublicEntryController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        // When someone clicks "View as Public" from your auth page:
        // it clears any previous role session and goes to the public site.
        [HttpGet("/ViewAsPublic")]
        public async Task<IActionResult> ViewAsPublic()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
            }

            return Redirect("/PublicMap");
        }
    }
}
