using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentProjectSystem.Data;
using StudentProjectSystem.Models;
using System.Security.Claims;

namespace StudentProjectSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db) => _db = db;

        // GET /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST /Account/Login
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Email sau parolă incorectă.");
                return View(model);
            }

            // Log the access
            _db.AuditLogs.Add(new AuditLog
            {
                Action    = "Login",
                Details   = $"User {user.Email} logged in",
                UserId    = user.UserId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name,           user.FullName),
                new Claim(ClaimTypes.Email,          user.Email),
                new Claim(ClaimTypes.Role,           user.Role)
            };

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var props     = new AuthenticationProperties { IsPersistent = model.RememberMe };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToDashboard();
        }

        // GET /Account/Logout
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    Action    = "Logout",
                    Details   = $"User logged out",
                    UserId    = userId,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _db.SaveChangesAsync();
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // GET /Account/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword() => View();

        // POST /Account/ChangePassword
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FindAsync(GetCurrentUserId());
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Parola curentă este incorectă.");
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.UpdatedAt    = DateTime.UtcNow;
            _db.AuditLogs.Add(new AuditLog { Action = "ChangePassword", UserId = user.UserId });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Parola a fost schimbată cu succes.";
            return RedirectToDashboard();
        }

        // GET /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _db.Users.FindAsync(GetCurrentUserId());
            if (user == null) return NotFound();
            return View(user);
        }

        public IActionResult AccessDenied() => View();

        // ── Helpers ──────────────────────────────────────
        private int GetCurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }

        private IActionResult RedirectToDashboard()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role switch
            {
                "Admin"     => RedirectToAction("Index", "Admin"),
                "Professor" => RedirectToAction("Index", "Professor"),
                _           => RedirectToAction("Index", "Student")
            };
        }
    }
}
