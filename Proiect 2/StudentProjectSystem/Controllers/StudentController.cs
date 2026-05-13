using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentProjectSystem.Data;
using StudentProjectSystem.Models;
using System.Security.Claims;

namespace StudentProjectSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public StudentController(AppDbContext db, IWebHostEnvironment env, IConfiguration config)
        {
            _db = db; _env = env; _config = config;
        }

        // GET /Student
        public async Task<IActionResult> Index()
        {
            int uid = GetCurrentUserId();

            var projects = await _db.Projects
                .Where(p => p.StudentId == uid)
                .Include(p => p.Grade)
                .OrderByDescending(p => p.UploadDate)
                .ToListAsync();

            var unread = await _db.Notifications.CountAsync(n => n.UserId == uid && !n.IsRead);

            var vm = new StudentDashboardViewModel
            {
                TotalProjects      = projects.Count,
                GradedProjects     = projects.Count(p => p.Grade != null),
                PendingProjects    = projects.Count(p => p.Grade == null),
                AverageGrade       = projects.Where(p => p.Grade != null).Any()
                                        ? projects.Where(p => p.Grade != null).Average(p => (double)p.Grade!.Score)
                                        : null,
                RecentProjects     = projects.Take(5).ToList(),
                UnreadNotifications = unread
            };

            return View(vm);
        }

        // GET /Student/Projects
        public async Task<IActionResult> Projects(string? search, string? status)
        {
            int uid = GetCurrentUserId();

            var query = _db.Projects
                .Where(p => p.StudentId == uid)
                .Include(p => p.Grade)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(await query.OrderByDescending(p => p.UploadDate).ToListAsync());
        }

        // GET /Student/ProjectDetails/5
        public async Task<IActionResult> ProjectDetails(int id)
        {
            int uid = GetCurrentUserId();
            var project = await _db.Projects
                .Include(p => p.Student)
                .Include(p => p.Grade).ThenInclude(g => g!.Professor)
                .Include(p => p.Comments).ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.StudentId == uid);

            if (project == null) return NotFound();
            return View(project);
        }

        // GET /Student/Upload
        [HttpGet]
        public IActionResult Upload() => View(new UploadProjectViewModel());

        // POST /Student/Upload
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadProjectViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int uid          = GetCurrentUserId();
            string? fileName = null;
            string? filePath = null;
            long?   fileSize = null;

            if (model.File != null && model.File.Length > 0)
            {
                var maxMB  = _config.GetValue<int>("FileUpload:MaxFileSizeMB", 50);
                var allowed = (_config.GetValue<string>("FileUpload:AllowedExtensions") ?? "").Split(',');
                var ext     = Path.GetExtension(model.File.FileName).ToLower();

                if (model.File.Length > maxMB * 1024 * 1024)
                {
                    ModelState.AddModelError("File", $"Fișierul depășește {maxMB} MB.");
                    return View(model);
                }
                if (!allowed.Contains(ext))
                {
                    ModelState.AddModelError("File", "Extensie de fișier nepermisă.");
                    return View(model);
                }

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsDir);
                fileName = $"{Guid.NewGuid()}{ext}";
                filePath = Path.Combine(uploadsDir, fileName);
                using var fs = System.IO.File.Create(filePath);
                await model.File.CopyToAsync(fs);
                fileSize = model.File.Length;
                filePath = $"/uploads/{fileName}";
            }

            var project = new Project
            {
                Title       = model.Title,
                Description = model.Description,
                Deadline    = model.Deadline,
                FileName    = model.File?.FileName,
                FilePath    = filePath,
                FileSize    = fileSize,
                StudentId   = uid,
                Status      = "Submitted"
            };

            _db.Projects.Add(project);
            _db.AuditLogs.Add(new AuditLog { Action = "UploadProject", Details = model.Title, UserId = uid });
            await _db.SaveChangesAsync();

            // Notify all professors
            var professors = await _db.Users.Where(u => u.Role == "Professor" && u.IsActive).ToListAsync();
            foreach (var prof in professors)
            {
                _db.Notifications.Add(new Notification
                {
                    Message   = $"Proiect nou încărcat: \"{project.Title}\"",
                    UserId    = prof.UserId,
                    ProjectId = project.ProjectId
                });
            }
            await _db.SaveChangesAsync();

            TempData["Success"] = "Proiectul a fost încărcat cu succes!";
            return RedirectToAction(nameof(Projects));
        }

        // GET /Student/EditProject/5
        [HttpGet]
        public async Task<IActionResult> EditProject(int id)
        {
            int uid = GetCurrentUserId();
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.ProjectId == id && p.StudentId == uid);
            if (project == null) return NotFound();
            if (project.Status == "Graded") { TempData["Error"] = "Nu poți edita un proiect evaluat."; return RedirectToAction(nameof(Projects)); }
            if (project.Deadline.HasValue && project.Deadline < DateTime.UtcNow) { TempData["Error"] = "Termenul limită a trecut."; return RedirectToAction(nameof(Projects)); }

            var vm = new EditProjectViewModel
            {
                ProjectId   = project.ProjectId,
                Title       = project.Title,
                Description = project.Description,
                Deadline    = project.Deadline
            };
            return View(vm);
        }

        // POST /Student/EditProject
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProject(EditProjectViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            int uid = GetCurrentUserId();
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId && p.StudentId == uid);
            if (project == null) return NotFound();

            project.Title       = model.Title;
            project.Description = model.Description;
            project.Deadline    = model.Deadline;

            if (model.File != null && model.File.Length > 0)
            {
                var ext      = Path.GetExtension(model.File.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{ext}";
                var dir      = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(dir);
                using var fs = System.IO.File.Create(Path.Combine(dir, fileName));
                await model.File.CopyToAsync(fs);
                project.FileName = model.File.FileName;
                project.FilePath = $"/uploads/{fileName}";
                project.FileSize = model.File.Length;
            }

            _db.AuditLogs.Add(new AuditLog { Action = "EditProject", Details = project.Title, UserId = uid });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Proiectul a fost actualizat.";
            return RedirectToAction(nameof(Projects));
        }

        // GET /Student/Notifications
        public async Task<IActionResult> Notifications()
        {
            int uid = GetCurrentUserId();
            var notifs = await _db.Notifications
                .Where(n => n.UserId == uid)
                .Include(n => n.Project)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Mark all as read
            foreach (var n in notifs.Where(n => !n.IsRead)) n.IsRead = true;
            await _db.SaveChangesAsync();

            return View(notifs);
        }

        // GET /Student/AddComment/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddCommentViewModel model)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(ProjectDetails), new { id = model.ProjectId });
            int uid = GetCurrentUserId();
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId && p.StudentId == uid);
            if (project == null) return NotFound();

            _db.Comments.Add(new Comment { Content = model.Content, ProjectId = model.ProjectId, AuthorId = uid });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ProjectDetails), new { id = model.ProjectId });
        }

        private int GetCurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }
    }
}
