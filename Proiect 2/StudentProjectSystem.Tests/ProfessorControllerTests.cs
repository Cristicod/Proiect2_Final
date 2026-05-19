using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StudentProjectSystem.Controllers;
using StudentProjectSystem.Models;
using StudentProjectSystem.Tests.Helpers;
using Xunit;

namespace StudentProjectSystem.Tests
{
    public class ProfessorControllerTests
    {
        private readonly IWebHostEnvironment _env;

        public ProfessorControllerTests()
        {
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns("wwwroot");
            _env = envMock.Object;
        }

        [Fact]
        public async Task Index_Returns_Correct_Pending_And_Graded_Counts()
        {
            // Test 14: Tabloul de bord al profesorului cu numărarea corectă a proiectelor din seed
            using var testDb = new TestDatabaseContext();
            var controller = new ProfessorController(testDb.DbContext, _env);
            
            // Autentificăm profesorul Ionescu Andrei (UserId = 2)
            ControllerTestHelper.SetupControllerContext(controller, 2, "Prof. Ionescu Andrei", "Professor", "ionescu@university.ro");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var vm = Assert.IsType<ProfessorDashboardViewModel>(viewResult.Model);

            // În seed-ul bazei de date avem 4 proiecte în total:
            // - Proiect 1: Evaluat (Graded, Nota 9.5)
            // - Proiect 2: Submitted (În așteptare)
            // - Proiect 3: UnderReview (În așteptare)
            // - Proiect 4: Submitted (În așteptare)
            Assert.Equal(4, vm.TotalProjects);
            Assert.Equal(1, vm.GradedProjects);
            Assert.Equal(3, vm.PendingProjects);
            Assert.Equal(0, vm.ArchivedProjects); // Niciunul nu este arhivat inițial
        }

        [Fact]
        public async Task GradeProject_Creates_NewGrade_And_Updates_Status_To_Graded()
        {
            // Test 15: Evaluare proiect - adăugare notă nouă, actualizare status în Graded și notificare automată student
            using var testDb = new TestDatabaseContext();
            var controller = new ProfessorController(testDb.DbContext, _env);
            ControllerTestHelper.SetupControllerContext(controller, 2, "Prof. Ionescu Andrei", "Professor", "ionescu@university.ro");

            // Evaluăm proiectul cu ID-ul 2 ("Platformă E-Learning", încărcat de studentul Mihai Georgescu - UserId = 4)
            var vm = new GradeProjectViewModel
            {
                ProjectId = 2,
                Score = 8.75m,
                Feedback = "Foarte bună arhitectura, implementare curată și corectă."
            };

            var result = await controller.GradeProject(vm);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ProjectDetails", redirectResult.ActionName);

            // Verificăm nota adăugată în DB
            var grade = testDb.DbContext.Grades.FirstOrDefault(g => g.ProjectId == 2);
            Assert.NotNull(grade);
            Assert.Equal(8.75m, grade.Score);
            Assert.Equal("Foarte bună arhitectura, implementare curată și corectă.", grade.Feedback);
            Assert.Equal(2, grade.ProfessorId);

            // Verificăm statusul actualizat al proiectului
            var project = testDb.DbContext.Projects.Find(2);
            Assert.NotNull(project);
            Assert.Equal("Graded", project.Status);

            // Verificăm trimiterea notificării către studentul Mihai Georgescu (UserId = 4)
            var notification = testDb.DbContext.Notifications.FirstOrDefault(n => n.UserId == 4 && n.ProjectId == 2);
            Assert.NotNull(notification);
            Assert.Equal("Proiectul \"Platformă E-Learning\" a primit nota 8.75.", notification.Message);
        }

        [Fact]
        public async Task ArchiveProject_Sets_IsArchived_And_Status()
        {
            // Test 16: Arhivarea unui proiect valid de către profesor
            using var testDb = new TestDatabaseContext();
            var controller = new ProfessorController(testDb.DbContext, _env);
            ControllerTestHelper.SetupControllerContext(controller, 2, "Prof. Ionescu Andrei", "Professor", "ionescu@university.ro");

            var result = await controller.ArchiveProject(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Projects", redirectResult.ActionName);

            // Verificăm modificările de arhivare pe proiect în DB
            var project = testDb.DbContext.Projects.Find(1);
            Assert.NotNull(project);
            Assert.True(project.IsArchived);
            Assert.Equal("Archived", project.Status);
        }
    }
}
