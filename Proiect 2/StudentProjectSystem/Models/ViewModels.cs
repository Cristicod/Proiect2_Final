using System.ComponentModel.DataAnnotations;

namespace StudentProjectSystem.Models
{
    // ── Auth ──────────────────────────────────────────────
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu")]
        [EmailAddress(ErrorMessage = "Format email invalid")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Parola este obligatorie")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Parola curentă")]
        public string CurrentPassword { get; set; } = "";

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parola nouă")]
        public string NewPassword { get; set; } = "";

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Parolele nu coincid")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmare parolă")]
        public string ConfirmPassword { get; set; } = "";
    }

    // ── Project ───────────────────────────────────────────
    public class UploadProjectViewModel
    {
        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [MaxLength(300)]
        [Display(Name = "Titlu proiect")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        [MaxLength(2000)]
        [Display(Name = "Descriere")]
        public string Description { get; set; } = "";

        [Display(Name = "Termen limită")]
        public DateTime? Deadline { get; set; }

        [Display(Name = "Fișier proiect")]
        public IFormFile? File { get; set; }
    }

    public class EditProjectViewModel
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu")]
        [MaxLength(300)]
        [Display(Name = "Titlu proiect")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Descrierea este obligatorie")]
        [MaxLength(2000)]
        [Display(Name = "Descriere")]
        public string Description { get; set; } = "";

        [Display(Name = "Termen limită")]
        public DateTime? Deadline { get; set; }

        [Display(Name = "Fișier nou (opțional)")]
        public IFormFile? File { get; set; }
    }

    // ── Grade ─────────────────────────────────────────────
    public class GradeProjectViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectTitle { get; set; } = "";
        public string StudentName { get; set; } = "";

        [Required(ErrorMessage = "Nota este obligatorie")]
        [Range(1, 10, ErrorMessage = "Nota trebuie să fie între 1 și 10")]
        [Display(Name = "Notă (1-10)")]
        public decimal Score { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Feedback")]
        public string? Feedback { get; set; }
    }

    // ── Comment ───────────────────────────────────────────
    public class AddCommentViewModel
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Comentariul nu poate fi gol")]
        [MaxLength(2000)]
        [Display(Name = "Comentariu")]
        public string Content { get; set; } = "";
    }

    // ── Admin User Management ─────────────────────────────
    public class CreateUserViewModel
    {
        [Required]
        [MaxLength(150)]
        [Display(Name = "Nume complet")]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parolă")]
        public string Password { get; set; } = "";

        [Required]
        [Display(Name = "Rol")]
        public string Role { get; set; } = "Student";
    }

    // ── Dashboard Stats ───────────────────────────────────
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalProfessors { get; set; }
        public int TotalProjects { get; set; }
        public int GradedProjects { get; set; }
        public int PendingProjects { get; set; }
        public List<AuditLog> RecentLogs { get; set; } = new();
        public List<User> RecentUsers { get; set; } = new();
    }

    public class ProfessorDashboardViewModel
    {
        public int TotalProjects { get; set; }
        public int PendingProjects { get; set; }
        public int GradedProjects { get; set; }
        public int ArchivedProjects { get; set; }
        public List<Project> RecentProjects { get; set; } = new();
        public int UnreadNotifications { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public int TotalProjects { get; set; }
        public int GradedProjects { get; set; }
        public int PendingProjects { get; set; }
        public double? AverageGrade { get; set; }
        public List<Project> RecentProjects { get; set; } = new();
        public int UnreadNotifications { get; set; }
    }
}
