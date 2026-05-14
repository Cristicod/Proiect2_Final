using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentProjectSystem.Data;
using StudentProjectSystem.Models;
using System.Security.Claims;

namespace StudentProjectSystem.Controllers
{
    [Authorize(Roles = "Professor")]
    public class ProfessorController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProfessorController(AppDbContext db, IWebHostEnvironment env)
        { _db = db; _env = env; }

        // GET /Professor
        public async Task<IActionResult> Index()
        {
            int uid = GetCurrentUserId();

            var allProjects = await _db.Projects
                .Include(p => p.Student)
                .Include(p => p.Grade)
                .Where(p => !p.IsArchived)
                .OrderByDescending(p => p.UploadDate)
                .ToListAsync();

            var unread = await _db.Notifications.CountAsync(n => n.UserId == uid && !n.IsRead);

            var vm = new ProfessorDashboardViewModel
            {
                TotalProjects    = allProjects.Count,
                PendingProjects  = allProjects.Count(p => p.Grade == null),
                GradedProjects   = allProjects.Count(p => p.Grade != null),
                ArchivedProjects = await _db.Projects.CountAsync(p => p.IsArchived),
                RecentProjects   = allProjects.Take(6).ToList(),
                UnreadNotifications = unread
            };
            return View(vm);
        }

        // GET /Professor/Projects
        public async Task<IActionResult> Projects(string? search, string? status, string? sort)
        {
            var query = _db.Projects
                .Include(p => p.Student)
                .Include(p => p.Grade)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Title.Contains(search)
                                      || p.Student!.FullName.Contains(search)
                                      || p.Description.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            query = sort switch
            {
                "title"   => query.OrderBy(p => p.Title),
                "student" => query.OrderBy(p => p.Student!.FullName),
                "grade"   => query.OrderByDescending(p => p.Grade != null ? p.Grade.Score : 0),
                _         => query.OrderByDescending(p => p.UploadDate)
            };

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Sort   = sort;

            return View(await query.ToListAsync());
        }

        // GET /Professor/ProjectDetails/5
        public async Task<IActionResult> ProjectDetails(int id)
        {
            var project = await _db.Projects
                .Include(p => p.Student)
                .Include(p => p.Grade).ThenInclude(g => g!.Professor)
                .Include(p => p.Comments).ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null) return NotFound();
            ViewBag.GradeVm = new GradeProjectViewModel
            {
                ProjectId    = project.ProjectId,
                ProjectTitle = project.Title,
                StudentName  = project.Student?.FullName ?? "",
                Score        = project.Grade?.Score ?? 5,
                Feedback     = project.Grade?.Feedback
            };
            return View(project);
        }

        // POST /Professor/GradeProject
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GradeProject(GradeProjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Date invalide. Verificați nota introdusă.";
                return RedirectToAction(nameof(ProjectDetails), new { id = model.ProjectId });
            }

            int uid = GetCurrentUserId();
            var project = await _db.Projects.Include(p => p.Grade).FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId);
            if (project == null) return NotFound();

            if (project.Grade != null)
            {
                // Update existing
                project.Grade.Score     = model.Score;
                project.Grade.Feedback  = model.Feedback;
                project.Grade.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // New grade
                _db.Grades.Add(new Grade
                {
                    Score       = model.Score,
                    Feedback    = model.Feedback,
                    ProjectId   = model.ProjectId,
                    ProfessorId = uid
                });
                project.Status = "Graded";
            }

            // Notify student
            _db.Notifications.Add(new Notification
            {
                Message   = $"Proiectul \"{project.Title}\" a primit nota {model.Score}.",
                UserId    = project.StudentId,
                ProjectId = project.ProjectId
            });

            _db.AuditLogs.Add(new AuditLog { Action = "GradeProject", Details = $"Project {project.ProjectId} scored {model.Score}", UserId = uid });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Nota a fost salvată cu succes!";
            return RedirectToAction(nameof(ProjectDetails), new { id = model.ProjectId });
        }

        // POST /Professor/AddComment
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddCommentViewModel model)
        {
            int uid = GetCurrentUserId();
            var project = await _db.Projects.FindAsync(model.ProjectId);
            if (project == null) return NotFound();

            _db.Comments.Add(new Comment { Content = model.Content, ProjectId = model.ProjectId, AuthorId = uid });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ProjectDetails), new { id = model.ProjectId });
        }

        // POST /Professor/ArchiveProject
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveProject(int id)
        {
            var project = await _db.Projects.FindAsync(id);
            if (project == null) return NotFound();
            project.IsArchived = true;
            project.Status     = "Archived";
            _db.AuditLogs.Add(new AuditLog { Action = "ArchiveProject", Details = project.Title, UserId = GetCurrentUserId() });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Proiectul a fost arhivat.";
            return RedirectToAction(nameof(Projects));
        }

        // GET /Professor/Notifications
        public async Task<IActionResult> Notifications()
        {
            int uid = GetCurrentUserId();
            var notifs = await _db.Notifications
                .Where(n => n.UserId == uid)
                .Include(n => n.Project)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            foreach (var n in notifs.Where(n => !n.IsRead)) n.IsRead = true;
            await _db.SaveChangesAsync();
            return View(notifs);
        }

        // GET /Professor/DownloadProject/5
        public async Task<IActionResult> DownloadProject(int id)
        {
            var project = await _db.Projects.FindAsync(id);
            if (project == null || string.IsNullOrEmpty(project.FilePath)) return NotFound();
            var path = Path.Combine(_env.WebRootPath, project.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(path)) return NotFound();
            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            return File(bytes, "application/octet-stream", project.FileName ?? "project");
        }

        private int GetCurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : 0;
        }
    }
}
