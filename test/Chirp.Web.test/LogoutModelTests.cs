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
            // Mock UserManager with minimal constructor arguments required by SignInManager
            var userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(),
                It.IsAny<IOptions<IdentityOptions>>(),
                It.IsAny<IPasswordHasher<IdentityUser>>(),
                It.IsAny<IEnumerable<IUserValidator<IdentityUser>>>(),
                It.IsAny<IEnumerable<IPasswordValidator<IdentityUser>>>(),
                It.IsAny<ILookupNormalizer>(),
                It.IsAny<IdentityErrorDescriber>(),
                It.IsAny<IServiceProvider>(),
                It.IsAny<ILogger<UserManager<IdentityUser>>>()
            );

            // HttpContext accessor required by SignInManager internals
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            contextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Claims principal factory dependency
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();

            // Identity options mock
            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());

            // Logger used internally by SignInManager
            var signInManagerLoggerMock = new Mock<ILogger<SignInManager<IdentityUser>>>();

            // Construct the SignInManager mock
            _signInManagerMock = new Mock<SignInManager<IdentityUser>>(
                userManagerMock.Object,
                contextAccessorMock.Object,
                claimsFactoryMock.Object,
                optionsMock.Object,
                signInManagerLoggerMock.Object,
                null!, // IAuthenticationSchemeProvider - not used in SignOutAsync
                null!  // IUserConfirmation<IdentityUser> - not required here
            );

            // Make SignOutAsync completable and verifiable
            _signInManagerMock.Setup(s => s.SignOutAsync())
                              .Returns(Task.CompletedTask)
                              .Verifiable();

            // Logger for the LogoutModel page
            _loggerMock = new Mock<ILogger<LogoutModel>>();

            // Dedicated HttpContext for the Razor Page (used for redirects)
            var pageHttpContext = new DefaultHttpContext();

            // Mock AuthenticationService because PageModel base calls it during logout
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Service provider that supplies the mocked authentication service
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            // Assign service provider to both contexts
            httpContext.RequestServices = serviceProviderMock.Object;
            pageHttpContext.RequestServices = serviceProviderMock.Object;

            // Instantiate the page model under test
            _logoutModel = new LogoutModel(_signInManagerMock.Object, _loggerMock.Object);

            // Provide PageContext with its own HttpContext for redirect handling
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

            // SignInManager.SignOutAsync must be called exactly once
            _signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once());

            // Logger must record the "User logged out." information message
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("User logged out.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once());

            // Result must be a local redirect pointing to the provided returnUrl
            var redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task OnPost_WithoutReturnUrl_RedirectsToPage_AndSignsOut()
        {
            var result = await _logoutModel.OnPost(null);

            // SignInManager.SignOutAsync must be called exactly once
            _signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once());

            // Logger must record the "User logged out." information message
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("User logged out.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once());

            // Result must be a redirect to the application's default post-logout page
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Public", redirectResult.PageName);
        }
    }
}