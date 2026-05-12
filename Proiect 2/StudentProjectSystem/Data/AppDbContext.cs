using Microsoft.EntityFrameworkCore;
using StudentProjectSystem.Models;

namespace StudentProjectSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.UserId);
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.Role).HasMaxLength(20);
            });

            // Project
            modelBuilder.Entity<Project>(e =>
            {
                e.HasKey(p => p.ProjectId);
                e.HasOne(p => p.Student)
                 .WithMany(u => u.Projects)
                 .HasForeignKey(p => p.StudentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Grade – one-to-one with Project
            modelBuilder.Entity<Grade>(e =>
            {
                e.HasKey(g => g.GradeId);
                e.HasIndex(g => g.ProjectId).IsUnique();
                e.HasOne(g => g.Project)
                 .WithOne(p => p.Grade)
                 .HasForeignKey<Grade>(g => g.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(g => g.Professor)
                 .WithMany(u => u.GradesGiven)
                 .HasForeignKey(g => g.ProfessorId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.Property(g => g.Score).HasColumnType("decimal(5,2)");
            });

            // Comment
            modelBuilder.Entity<Comment>(e =>
            {
                e.HasKey(c => c.CommentId);
                e.HasOne(c => c.Project)
                 .WithMany(p => p.Comments)
                 .HasForeignKey(c => c.ProjectId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(c => c.Author)
                 .WithMany(u => u.Comments)
                 .HasForeignKey(c => c.AuthorId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Notification
            modelBuilder.Entity<Notification>(e =>
            {
                e.HasKey(n => n.NotificationId);
                e.HasOne(n => n.User)
                 .WithMany(u => u.Notifications)
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(n => n.Project)
                 .WithMany(p => p.Notifications)
                 .HasForeignKey(n => n.ProjectId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // AuditLog
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.HasKey(l => l.LogId);
                e.HasOne(l => l.User)
                 .WithMany(u => u.AuditLogs)
                 .HasForeignKey(l => l.UserId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Hashed "Password123!" via BCrypt (cost 11)
            var adminHash   = BCrypt.Net.BCrypt.HashPassword("Password123!");
            var profHash    = BCrypt.Net.BCrypt.HashPassword("Password123!");
            var studentHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

            modelBuilder.Entity<User>().HasData(
                new User { UserId = 1, FullName = "Administrator System", Email = "admin@university.ro",   PasswordHash = adminHash,   Role = "Admin",     IsActive = true, CreatedAt = new DateTime(2026,1,1), UpdatedAt = new DateTime(2026,1,1) },
                new User { UserId = 2, FullName = "Prof. Ionescu Andrei",  Email = "ionescu@university.ro", PasswordHash = profHash,    Role = "Professor", IsActive = true, CreatedAt = new DateTime(2026,1,1), UpdatedAt = new DateTime(2026,1,1) },
                new User { UserId = 3, FullName = "Prof. Popescu Maria",   Email = "popescu@university.ro", PasswordHash = profHash,    Role = "Professor", IsActive = true, CreatedAt = new DateTime(2026,1,1), UpdatedAt = new DateTime(2026,1,1) },
                new User { UserId = 4, FullName = "Mihai Georgescu",       Email = "mihai.g@student.ro",    PasswordHash = studentHash, Role = "Student",   IsActive = true, CreatedAt = new DateTime(2026,1,1), UpdatedAt = new DateTime(2026,1,1) },
                new User { UserId = 5, FullName = "Ana Constantin",        Email = "ana.c@student.ro",      PasswordHash = studentHash, Role = "Student",   IsActive = true, CreatedAt = new DateTime(2026,1,1), UpdatedAt = new DateTime(2026,1,1) },
                new User { UserId = 6, FullName = "Radu Florescu",         Email = "radu.f@student.ro",     PasswordHash = studentHash, Role = "Student",   IsActive = true, CreatedAt = new DateTime(2026,1,1), UpdatedAt = new DateTime(2026,1,1) }
            );

            modelBuilder.Entity<Project>().HasData(
                new Project { ProjectId = 1, Title = "Aplicație Web de Gestionare Bibliotecă", Description = "Sistem web pentru gestionarea împrumuturilor de cărți dintr-o bibliotecă.", StudentId = 4, Status = "Graded",    UploadDate = new DateTime(2026,2,15), IsArchived = false },
                new Project { ProjectId = 2, Title = "Platformă E-Learning",                   Description = "Platformă online pentru cursuri și materiale educative interactive.",       StudentId = 4, Status = "Submitted", UploadDate = new DateTime(2026,3,1),  IsArchived = false },
                new Project { ProjectId = 3, Title = "Sistem de Monitorizare IoT",             Description = "Aplicație pentru monitorizarea dispozitivelor IoT în timp real.",           StudentId = 5, Status = "UnderReview", UploadDate = new DateTime(2026,2,20), IsArchived = false },
                new Project { ProjectId = 4, Title = "Chatbot AI pentru Suport",               Description = "Chatbot bazat pe machine learning pentru servicii de suport tehnic.",       StudentId = 6, Status = "Submitted", UploadDate = new DateTime(2026,3,5),  IsArchived = false }
            );

            modelBuilder.Entity<Grade>().HasData(
                new Grade { GradeId = 1, Score = 9.5m, Feedback = "Proiect excelent! Implementare corectă și interfață bine realizată.", ProjectId = 1, ProfessorId = 2, GradedAt = new DateTime(2026,2,28), UpdatedAt = new DateTime(2026,2,28) }
            );

            modelBuilder.Entity<Comment>().HasData(
                new Comment { CommentId = 1, Content = "Felicitări pentru structura codului! Câteva optimizări minore pot fi adăugate.", ProjectId = 1, AuthorId = 2, CreatedAt = new DateTime(2026,2,25) },
                new Comment { CommentId = 2, Content = "Documentația este bine realizată.", ProjectId = 3, AuthorId = 2, CreatedAt = new DateTime(2026,3,1) }
            );
        }
    }
}
