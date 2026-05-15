using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StudentProjectSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                return role switch
                {
                    "Admin"     => RedirectToAction("Index", "Admin"),
                    "Professor" => RedirectToAction("Index", "Professor"),
                    _           => RedirectToAction("Index", "Student")
                };
            }
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Error() => View();
    }
}
