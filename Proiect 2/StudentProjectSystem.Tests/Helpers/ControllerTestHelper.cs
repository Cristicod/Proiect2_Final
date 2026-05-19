using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace StudentProjectSystem.Tests.Helpers
{
    public static class ControllerTestHelper
    {
        public static void SetupControllerContext(Controller controller, int userId, string userName, string userRole, string userEmail)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, userRole),
                new Claim(ClaimTypes.Email, userEmail)
            };
            
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var principal = new ClaimsPrincipal(identity);

            // Simulăm IAuthenticationService (folosit pentru SignInAsync / SignOutAsync)
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Simulăm ServiceProvider pentru a returna IAuthenticationService din HttpContext.RequestServices
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            var httpContext = new DefaultHttpContext
            {
                User = principal,
                RequestServices = serviceProviderMock.Object
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Simulăm ITempDataDictionary
            var tempDataProvider = new Mock<ITempDataProvider>();
            controller.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

            // Simulăm IUrlHelper (pentru redirectionări locale)
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(true);
            controller.Url = urlHelperMock.Object;
        }
    }
}
