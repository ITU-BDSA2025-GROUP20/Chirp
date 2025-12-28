// test/Chirp.Web.test/LogoutModelTests.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Areas.Identity.Pages.Account;

namespace Tests.Web
{
    public class LogoutModelTests
    {
        private readonly Mock<SignInManager<IdentityUser>> _signInManagerMock;
        private readonly Mock<ILogger<LogoutModel>> _loggerMock;
        private readonly LogoutModel _logoutModel;

        public LogoutModelTests()
        {
            // Minimal mocks for SignInManager constructor
            var userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);

            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            contextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();

            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());

            var signInManagerLoggerMock = new Mock<ILogger<SignInManager<IdentityUser>>>();

            // Fully mock SignInManager
            _signInManagerMock = new Mock<SignInManager<IdentityUser>>(
                userManagerMock.Object,
                contextAccessorMock.Object,
                claimsFactoryMock.Object,
                optionsMock.Object,
                signInManagerLoggerMock.Object,
                null, // IAuthenticationSchemeProvider
                null  // IUserConfirmation<IdentityUser>
            );

            _signInManagerMock.Setup(s => s.SignOutAsync())
                              .Returns(Task.CompletedTask)
                              .Verifiable();

            _loggerMock = new Mock<ILogger<LogoutModel>>();

            // Create a separate HttpContext for the PageContext
            var pageHttpContext = new DefaultHttpContext();

            // === Mock IAuthenticationService to prevent ArgumentNullException ===
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            // Apply to both HttpContexts
            httpContext.RequestServices = serviceProviderMock.Object;
            pageHttpContext.RequestServices = serviceProviderMock.Object;

            // Instantiate LogoutModel
            _logoutModel = new LogoutModel(_signInManagerMock.Object, _loggerMock.Object);

            // Required for redirects
            _logoutModel.PageContext = new PageContext
            {
                HttpContext = pageHttpContext
            };
        }

        [Fact]
        public async Task OnPost_WithReturnUrl_RedirectsToReturnUrl_AndSignsOut()
        {
            const string returnUrl = "/some/page";

            var result = await _logoutModel.OnPost(returnUrl);

            _signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once());

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("User logged out.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once());

            var redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task OnPost_WithoutReturnUrl_RedirectsToPage_AndSignsOut()
        {
            var result = await _logoutModel.OnPost(null);

            _signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once());

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("User logged out.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once());

            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Public", redirectResult.PageName); // Matches actual app behavior
        }
    }
}