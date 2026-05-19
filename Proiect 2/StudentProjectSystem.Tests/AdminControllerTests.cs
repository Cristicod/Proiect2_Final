using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StudentProjectSystem.Controllers;
using StudentProjectSystem.Models;
using StudentProjectSystem.Tests.Helpers;
using Xunit;

namespace StudentProjectSystem.Tests
{
    public class AdminControllerTests
    {
        [Fact]
        public async Task Users_Filters_By_Search_And_Role()
        {
            // Test 17: Căutare și filtrare utilizatori în panoul de administrare
            using var testDb = new TestDatabaseContext();
            var controller = new AdminController(testDb.DbContext);
            ControllerTestHelper.SetupControllerContext(controller, 1, "Administrator System", "Admin", "admin@university.ro");

            var result = await controller.Users("ionescu", "Professor");

            var viewResult = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<User>>(viewResult.Model).ToList();

            // Trebuie să găsească exact 1 profesor conform textului căutat (Prof. Ionescu Andrei)
            Assert.Single(list);
            Assert.Equal("ionescu@university.ro", list.First().Email);
        }

        [Fact]
        public async Task ToggleUser_Changes_IsActive_State()
        {
            // Test 18: Activare/Dezactivare cont de către Admin cu înregistrare automată în tabela de audit (AuditLogs)
            using var testDb = new TestDatabaseContext();
            var controller = new AdminController(testDb.DbContext);
            ControllerTestHelper.SetupControllerContext(controller, 1, "Administrator System", "Admin", "admin@university.ro");

            // Utilizatorul cu ID = 4 (Mihai Georgescu) este activ inițial (IsActive = true)
            var userBefore = testDb.DbContext.Users.Find(4);
            Assert.NotNull(userBefore);
            Assert.True(userBefore.IsActive);

            var result = await controller.ToggleUser(4);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Users", redirectResult.ActionName);

            // Verificăm dacă starea a fost comutată pe inactiv (false)
            var userAfter = testDb.DbContext.Users.Find(4);
            Assert.NotNull(userAfter);
            Assert.False(userAfter.IsActive);

            // Verificăm dacă s-a scris corect logul de audit în DB
            var log = testDb.DbContext.AuditLogs.OrderByDescending(l => l.CreatedAt).FirstOrDefault();
            Assert.NotNull(log);
            Assert.Equal("DeactivateUser", log.Action);
            Assert.Equal(userAfter.Email, log.Details);
            Assert.Equal(1, log.UserId); // ID-ul administratorului care a efectuat acțiunea
        }

        [Fact]
        public async Task DeleteProject_Removes_Project_From_Database()
        {
            // Test 19: Ștergerea definitivă a unui proiect de către administrator și scrierea în logs
            using var testDb = new TestDatabaseContext();
            var controller = new AdminController(testDb.DbContext);
            ControllerTestHelper.SetupControllerContext(controller, 1, "Administrator System", "Admin", "admin@university.ro");

            // Verificăm existența proiectului cu ID-ul 1
            var projectBefore = testDb.DbContext.Projects.Find(1);
            Assert.NotNull(projectBefore);

            var result = await controller.DeleteProject(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Projects", redirectResult.ActionName);

            // Verificăm dispariția proiectului din baza de date
            var projectAfter = testDb.DbContext.Projects.Find(1);
            Assert.Null(projectAfter);

            // Verificăm scrierea în logul de audit
            var log = testDb.DbContext.AuditLogs.OrderByDescending(l => l.CreatedAt).FirstOrDefault();
            Assert.NotNull(log);
            Assert.Equal("DeleteProject", log.Action);
            Assert.Equal("Aplicație Web de Gestionare Bibliotecă", log.Details);
        }
    }
}
