using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using StudentProjectSystem.Models;
using Xunit;

namespace StudentProjectSystem.Tests
{
    public class ModelTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }

        [Fact]
        public void User_Validation_Default_Role_Is_Student()
        {
            // Test 1: Verifică rolul implicit și starea activă a unui utilizator nou creat
            var user = new User();
            Assert.Equal("Student", user.Role);
            Assert.True(user.IsActive);
        }

        [Fact]
        public void User_Validation_Requires_FullName_And_Email()
        {
            // Test 2: Validează că FullName și Email sunt obligatorii
            var user = new User { FullName = "", Email = "" };
            var errors = ValidateModel(user);
            
            Assert.Contains(errors, e => e.MemberNames.Contains("FullName"));
            Assert.Contains(errors, e => e.MemberNames.Contains("Email"));
        }

        [Fact]
        public void Project_Validation_Default_Status_Is_Submitted()
        {
            // Test 3: Verifică starea implicită și arhivarea unui proiect nou
            var project = new Project();
            Assert.Equal("Submitted", project.Status);
            Assert.False(project.IsArchived);
        }

        [Fact]
        public void Grade_Validation_Score_Range_1_to_10()
        {
            // Test 4: Verifică dacă nota este cuprinsă în intervalul valid [1, 10]
            var gradeExcedat = new Grade { Score = 11.5m, ProjectId = 1, ProfessorId = 2 };
            var errorsExcedat = ValidateModel(gradeExcedat);
            Assert.Contains(errorsExcedat, e => e.MemberNames.Contains("Score"));

            var gradeSublimit = new Grade { Score = 0.5m, ProjectId = 1, ProfessorId = 2 };
            var errorsSublimit = ValidateModel(gradeSublimit);
            Assert.Contains(errorsSublimit, e => e.MemberNames.Contains("Score"));

            var gradeValid = new Grade { Score = 9.5m, ProjectId = 1, ProfessorId = 2 };
            var errorsValid = ValidateModel(gradeValid);
            Assert.DoesNotContain(errorsValid, e => e.MemberNames.Contains("Score"));
        }

        [Fact]
        public void Notification_Validation_Default_IsRead_Is_False()
        {
            // Test 5: Verifică că notificările nou create sunt setate implicit ca necitite
            var notification = new Notification();
            Assert.False(notification.IsRead);
        }
    }
}
