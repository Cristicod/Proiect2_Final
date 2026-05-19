using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using StudentProjectSystem.Controllers;
using StudentProjectSystem.Models;
using StudentProjectSystem.Tests.Helpers;
using Xunit;

namespace StudentProjectSystem.Tests
{
    public class StudentControllerTests
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public StudentControllerTests()
        {
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns("wwwroot");
            _env = envMock.Object;

            var inMemorySettings = new Dictionary<string, string> {
                {"FileUpload:MaxFileSizeMB", "50"},
                {"FileUpload:AllowedExtensions", ".pdf,.zip,.rar"}
            };

            // Folosim un furnizor de configurare în memorie real, prevenind erorile cu metodele de extensie Mock-uite
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task Index_Returns_ViewResult_With_Correct_DashboardViewModel()
        {
            // Test 11: Vizualizarea dashboardului studentului cu statistici corecte de seed
            using var testDb = new TestDatabaseContext();
            var controller = new StudentController(testDb.DbContext, _env, _config);
            
            // Autentificăm studentul Mihai Georgescu (UserId = 4)
            ControllerTestHelper.SetupControllerContext(controller, 4, "Mihai Georgescu", "Student", "mihai.g@student.ro");

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var vm = Assert.IsType<StudentDashboardViewModel>(viewResult.Model);

            // Conform seed-ului din AppDbContext: Mihai Georgescu are 2 proiecte:
            // - ProiectId = 1 (Graded, Nota 9.5)
            // - ProiectId = 2 (Submitted, Fără notă)
            Assert.Equal(2, vm.TotalProjects);
            Assert.Equal(1, vm.GradedProjects);
            Assert.Equal(1, vm.PendingProjects);
            Assert.Equal(9.5, vm.AverageGrade);
        }

        [Fact]
        public async Task Projects_Filters_By_SearchString()
        {
            // Test 12: Căutare/filtrare proiecte student după titlu sau descriere
            using var testDb = new TestDatabaseContext();
            var controller = new StudentController(testDb.DbContext, _env, _config);
            ControllerTestHelper.SetupControllerContext(controller, 4, "Mihai Georgescu", "Student", "mihai.g@student.ro");

            var result = await controller.Projects("Bibliotecă", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<Project>>(viewResult.Model).ToList();

            // Trebuie să returneze exact 1 proiect (cel cu titlul "Aplicație Web de Gestionare Bibliotecă")
            Assert.Single(list);
            Assert.Equal("Aplicație Web de Gestionare Bibliotecă", list.First().Title);
        }

        [Fact]
        public async Task Upload_Adds_NewProject_And_Notifies_Professors()
        {
            // Test 13: Încărcare proiect nou, salvare în DB și trimitere notificări către profesori
            using var testDb = new TestDatabaseContext();
            var controller = new StudentController(testDb.DbContext, _env, _config);
            ControllerTestHelper.SetupControllerContext(controller, 4, "Mihai Georgescu", "Student", "mihai.g@student.ro");

            // Simulăm un fișier încărcat
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("rezervari_hotel.zip");
            fileMock.Setup(f => f.Length).Returns(2 * 1024 * 1024); // 2 MB

            var vm = new UploadProjectViewModel
            {
                Title = "Sistem de Rezervări Hotel",
                Description = "Aplicație desktop de gestiune hotelieră în C#.",
                File = fileMock.Object
            };

            var result = await controller.Upload(vm);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Projects", redirectResult.ActionName);

            // Validăm prezența noului proiect în DB
            var project = testDb.DbContext.Projects.FirstOrDefault(p => p.Title == "Sistem de Rezervări Hotel");
            Assert.NotNull(project);
            Assert.Equal(4, project.StudentId);
            Assert.Equal("Submitted", project.Status);

            // Verificăm dacă profesorii din seed (UserId = 2 și UserId = 3) au primit notificări referitoare la încărcarea proiectului
            var notifications = testDb.DbContext.Notifications.Where(n => n.ProjectId == project.ProjectId).ToList();
            Assert.Equal(2, notifications.Count);
            Assert.Contains(notifications, n => n.UserId == 2);
            Assert.Contains(notifications, n => n.UserId == 3);
        }
    }
}
