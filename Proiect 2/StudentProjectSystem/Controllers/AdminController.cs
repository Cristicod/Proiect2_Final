using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentProjectSystem.Data;
using StudentProjectSystem.Models;
using System.Security.Claims;

namespace StudentProjectSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db) => _db = db;

        // GET /Admin
        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalStudents   = await _db.Users.CountAsync(u => u.Role == "Student"),
                TotalProfessors = await _db.Users.CountAsync(u => u.Role == "Professor"),
                TotalProjects   = await _db.Projects.CountAsync(),
                GradedProjects  = await _db.Projects.CountAsync(p => p.Status == "Graded"),
                PendingProjects = await _db.Projects.CountAsync(p => p.Status == "Submitted"),
                RecentLogs      = await _db.AuditLogs.Include(l => l.User).OrderByDescending(l => l.CreatedAt).Take(10).ToListAsync(),
                RecentUsers     = await _db.Users.OrderByDescending(u => u.CreatedAt).Take(5).ToListAsync()
            };
            return View(vm);
        }

        // GET /Admin/Users
        public async Task<IActionResult> Users(string? search, string? role)
        {
            var query = _db.Users.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            ViewBag.Search = search;
            ViewBag.Role   = role;
            return View(await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync());
        }

        // GET /Admin/CreateUser
        [HttpGet]
        public IActionResult CreateUser() => View(new CreateUserViewModel());

        // POST /Admin/CreateUser
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (await _db.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email deja înregistrat.");
                return View(model);
            }

            var user = new User
            {
                FullName     = model.FullName,
                Email        = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role         = model.Role
            };
            _db.Users.Add(user);
            _db.AuditLogs.Add(new AuditLog { Action = "CreateUser", Details = $"{model.Email} ({model.Role})", UserId = GetCurrentUserId() });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Utilizatorul {model.FullName} a fost creat.";
            return RedirectToAction(nameof(Users));
        }

        // POST /Admin/ToggleUser
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive  = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            _db.AuditLogs.Add(new AuditLog { Action = user.IsActive ? "ActivateUser" : "DeactivateUser", Details = user.Email, UserId = GetCurrentUserId() });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Contul {user.Email} a fost {(user.IsActive ? "activat" : "dezactivat")}.";
            return RedirectToAction(nameof(Users));
        }

        // POST /Admin/ChangeUserPassword
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserPassword(int id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Parola trebuie să aibă minim 6 caractere.";
                return RedirectToAction(nameof(Users));
            }
            
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            _db.AuditLogs.Add(new AuditLog { Action = "ChangeUserPassword", Details = $"Admin changed password for {user.Email}", UserId = GetCurrentUserId() });
            await _db.SaveChangesAsync();
            
            TempData["Success"] = $"Parola pentru {user.FullName} a fost schimbată cu succes.";
            return RedirectToAction(nameof(Users));
        }

        // POST /Admin/DeleteProject
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _db.Projects.FindAsync(id);
            if (project == null) return NotFound();
            _db.Projects.Remove(project);
            _db.AuditLogs.Add(new AuditLog { Action = "DeleteProject", Details = project.Title, UserId = GetCurrentUserId() });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Proiectul a fost șters.";
            return RedirectToAction(nameof(Projects));
        }

        // GET /Admin/Projects
        public async Task<IActionResult> Projects(string? search)
        {
            var query = _db.Projects.Include(p => p.Student).Include(p => p.Grade).AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Title.Contains(search) || p.Student!.FullName.Contains(search));
            ViewBag.Search = search;
            return View(await query.OrderByDescending(p => p.UploadDate).ToListAsync());
        }

        // GET /Admin/Logs
        public async Task<IActionResult> Logs(int page = 1, int pageSize = 30)
        {
            var total = await _db.AuditLogs.CountAsync();
            var logs  = await _db.AuditLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page      = page;
            ViewBag.PageSize  = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            return View(logs);
        }

        private int GetCurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }
    }
}
