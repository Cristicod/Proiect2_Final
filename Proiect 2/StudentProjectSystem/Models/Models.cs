using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentProjectSystem.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = "";

        [Required, MaxLength(200), EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Student"; // Student | Professor | Admin

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<Grade> GradesGiven { get; set; } = new List<Grade>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }

    public class Project
    {
        public int ProjectId { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = "";

        [Required, MaxLength(2000)]
        public string Description { get; set; } = "";

        [MaxLength(500)]
        public string? FileName { get; set; }

        [MaxLength(1000)]
        public string? FilePath { get; set; }

        public long? FileSize { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public DateTime? Deadline { get; set; }

        public string Status { get; set; } = "Submitted"; // Submitted | UnderReview | Graded | Archived

        public bool IsArchived { get; set; } = false;

        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public User? Student { get; set; }

        // Navigation
        public Grade? Grade { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }

    public class Grade
    {
        public int GradeId { get; set; }

        [Range(1, 10)]
        public decimal Score { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        public DateTime GradedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int ProjectId { get; set; }
        public int ProfessorId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }

        [ForeignKey(nameof(ProfessorId))]
        public User? Professor { get; set; }
    }

    public class Comment
    {
        public int CommentId { get; set; }

        [Required, MaxLength(2000)]
        public string Content { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ProjectId { get; set; }
        public int AuthorId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }

        [ForeignKey(nameof(AuthorId))]
        public User? Author { get; set; }
    }

    public class Notification
    {
        public int NotificationId { get; set; }

        [Required, MaxLength(500)]
        public string Message { get; set; } = "";

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public int? ProjectId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }
    }

    public class AuditLog
    {
        public int LogId { get; set; }

        [Required, MaxLength(200)]
        public string Action { get; set; } = "";

        [MaxLength(1000)]
        public string? Details { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
