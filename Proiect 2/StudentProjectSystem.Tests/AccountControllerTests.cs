using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StudentProjectSystem.Controllers;
using StudentProjectSystem.Models;
using StudentProjectSystem.Tests.Helpers;
using Xunit;

namespace StudentProjectSystem.Tests
{
    public class AccountControllerTests
    {
        [Fact]
        public async Task Login_Returns_RedirectToDashboard_For_Valid_Credentials()
        {
            // Test 6: Autentificare validă cu email și parolă seed-uite
            using var testDb = new TestDatabaseContext();
            var controller = new AccountController(testDb.DbContext);
            ControllerTestHelper.SetupControllerContext(controller, 0, "", "", "");

            var model = new LoginViewModel
            {
                Email = "mihai.g@student.ro",
                Password = "Password123!"
            };

            var result = await controller.Login(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Student", redirectResult.ControllerName); // Redirecționează către dashboard-ul specific rolului (Student implicit)
        }

        [Fact]
        public async Task Login_Adds_ModelError_For_Invalid_Password()
        {
            // Test 7: Autentificare eșuată datorită parolei introduse greșit
            using var testDb = new TestDatabaseContext();
            var controller = new AccountController(testDb.DbContext);
            ControllerTestHelper.SetupControllerContext(controller, 0, "", "", "");

            var model = new LoginViewModel
            {
                Email = "mihai.g@student.ro",
                Password = "WrongPassword!"
            };

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage == "Email sau parolă incorectă."));
        }

        [Fact]
        public async Task Login_Adds_ModelError_For_NonExistent_Email()
        {
            // Test 8: Autentificare eșuată deoarece adresa de email nu există în baza de date
            using var testDb = new TestDatabaseContext();
            var controller = new AccountController(testDb.DbContext);
            ControllerTestHelper.SetupControllerContext(controller, 0, "", "", "");

            var model = new LoginViewModel
            {
                Email = "nonexistent@student.ro",
                Password = "Password123!"
            };

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage == "Email sau parolă incorectă."));
        }

        [Fact]
        public async Task ChangePassword_Succeeds_For_Correct_CurrentPassword()
        {
            // Test 9: Schimbarea parolei cu succes și stocarea hash-ului securizat în baza de date
            using var testDb = new TestDatabaseContext();
            var controller = new AccountController(testDb.DbContext);
            
            // Setăm utilizatorul curent ca fiind Mihai Georgescu (UserId = 4, rol = Student)
            ControllerTestHelper.SetupControllerContext(controller, 4, "Mihai Georgescu", "Student", "mihai.g@student.ro");

            var model = new ChangePasswordViewModel
            {
                CurrentPassword = "Password123!",
                NewPassword = "NewSecurePassword123!",
                ConfirmPassword = "NewSecurePassword123!"
            };

            var result = await controller.ChangePassword(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("mihai.g@student.ro", testDb.DbContext.Users.Find(4)?.Email);
            Assert.Equal("Parola a fost schimbată cu succes.", controller.TempData["Success"]);

            // Validăm criptarea bcrypt a noii parole în DB
            var user = testDb.DbContext.Users.Find(4);
            Assert.NotNull(user);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewSecurePassword123!", user.PasswordHash));
        }

        [Fact]
        public async Task ChangePassword_Fails_For_Incorrect_CurrentPassword()
        {
            // Test 10: Eșec schimbare parolă din cauza parolei curente greșite
            using var testDb = new TestDatabaseContext();
            var controller = new AccountController(testDb.DbContext);
            ControllerTestHelper.SetupControllerContext(controller, 4, "Mihai Georgescu", "Student", "mihai.g@student.ro");

            var model = new ChangePasswordViewModel
            {
                CurrentPassword = "WrongPassword!",
                NewPassword = "NewSecurePassword123!",
                ConfirmPassword = "NewSecurePassword123!"
            };

            var result = await controller.ChangePassword(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState["CurrentPassword"]!.Errors, e => e.ErrorMessage == "Parola curentă este incorectă.");
        }
    }
}
