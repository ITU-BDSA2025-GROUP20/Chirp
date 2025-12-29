// test/Chirp.Web.test/LoginModelTests.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Tests the LoginModel Razor Page for handling user login, including external logins and two-factor authentication.
namespace Tests.Web
{
    public class LoginModelTests
    {
        private readonly Mock<SignInManager<IdentityUser>> _signInManagerMock;
        private readonly Mock<ILogger<LoginModel>> _loggerMock;
        private readonly Mock<IUrlHelper> _urlHelperMock;
        private readonly Mock<ITempDataDictionary> _tempDataMock;
        private readonly LoginModel _loginModel;

        // Initializes mocks for dependencies and configures LoginModel with minimal required context.
        public LoginModelTests()
        {
            // UserManager requires multiple parameters; nulls are safe for mocks in tests.
            var userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(),
                null!, null!, null!, null!, null!, null!, null!, null!
            );

            // SignInManager handles authentication logic, mocked to isolate LoginModel behavior.
            _signInManagerMock = new Mock<SignInManager<IdentityUser>>(
                userManagerMock.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(),
                null!, null!, null!, null!
            );

            _loggerMock = new Mock<ILogger<LoginModel>>();

            // UrlHelper mock returns "/" for home to simulate typical routing behavior.
            _urlHelperMock = new Mock<IUrlHelper>();
            _urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

            _tempDataMock = new Mock<ITempDataDictionary>();

            // Set up LoginModel with mocked dependencies and basic HTTP context.
            _loginModel = new LoginModel(_signInManagerMock.Object, _loggerMock.Object)
            {
                PageContext = new PageContext
                {
                    HttpContext = new DefaultHttpContext()
                },
                Url = _urlHelperMock.Object,
                TempData = _tempDataMock.Object
            };
        }

        // Configures mock for IAuthenticationService to handle external scheme sign-out.
        private void SetupAuthenticationServiceMock()
        {
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, null!))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            _loginModel.PageContext.HttpContext.RequestServices = serviceProviderMock.Object;
        }

        #region OnGetAsync Tests

        // Verifies that an error message in query string is added to ModelState.
        [Fact]
        public async Task OnGetAsync_WithErrorMessage_AddsModelError()
        {
            SetupAuthenticationServiceMock();
            _loginModel.ErrorMessage = "Test error";

            await _loginModel.OnGetAsync();

            Assert.False(_loginModel.ModelState.IsValid);
            var error = Assert.Single(_loginModel.ModelState[string.Empty]!.Errors);
            Assert.Equal("Test error", error.ErrorMessage);
        }

        // Ensures ReturnUrl defaults to home ("/") when not provided.
        [Fact]
        public async Task OnGetAsync_DefaultsReturnUrlToHome()
        {
            SetupAuthenticationServiceMock();

            await _loginModel.OnGetAsync();

            Assert.Equal("/", _loginModel.ReturnUrl);
        }

        // Confirms that a provided ReturnUrl is preserved.
        [Fact]
        public async Task OnGetAsync_UsesProvidedReturnUrl()
        {
            SetupAuthenticationServiceMock();

            await _loginModel.OnGetAsync("/custom-return");

            Assert.Equal("/custom-return", _loginModel.ReturnUrl);
        }

        // Tests that external authentication schemes are signed out on page load (e.g., for external login cleanup).
        [Fact]
        public async Task OnGetAsync_SignsOutExternalScheme()
        {
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, null!))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            _loginModel.PageContext.HttpContext.RequestServices = serviceProviderMock.Object;

            await _loginModel.OnGetAsync();

            authServiceMock.Verify();
        }

        // Verifies that external login providers (e.g., Google) are loaded correctly.
        [Fact]
        public async Task OnGetAsync_LoadsExternalLogins()
        {
            SetupAuthenticationServiceMock();

            var schemes = new List<AuthenticationScheme>
            {
                new AuthenticationScheme("Google", "Google", typeof(CookieAuthenticationHandler))
            };

            _signInManagerMock
                .Setup(s => s.GetExternalAuthenticationSchemesAsync())
                .ReturnsAsync(schemes);

            await _loginModel.OnGetAsync();

            Assert.Equal(schemes, _loginModel.ExternalLogins);
        }

        #endregion

        #region OnPostAsync Tests

        // Ensures invalid model state (e.g., missing email) returns the login page.
        [Fact]
        public async Task OnPostAsync_InvalidModelState_ReturnsPage()
        {
            _loginModel.ModelState.AddModelError("Test", "Invalid");

            var result = await _loginModel.OnPostAsync();

            Assert.IsType<PageResult>(result);
        }

        // Tests successful login redirects to specified ReturnUrl and logs success.
        [Fact]
        public async Task OnPostAsync_SuccessfulLogin_RedirectsToReturnUrl()
        {
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "password",
                RememberMe = true
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync("test@example.com", "password", true, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var result = await _loginModel.OnPostAsync("/custom");

            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/custom", redirect.Url);

            _loggerMock.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null!,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        // Verifies successful login defaults to home ("/") when no ReturnUrl is provided.
        [Fact]
        public async Task OnPostAsync_DefaultReturnUrlOnSuccess()
        {
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "password"
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var result = await _loginModel.OnPostAsync();

            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/", redirect.Url);
        }

        // Tests two-factor authentication requirement redirects to 2FA page with correct parameters.
        [Fact]
        public async Task OnPostAsync_RequiresTwoFactor_RedirectsTo2faPage()
        {
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "password",
                RememberMe = true
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), true, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

            var result = await _loginModel.OnPostAsync("/return");

            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./LoginWith2fa", redirect.PageName);

            Assert.True(redirect.RouteValues!.TryGetValue("ReturnUrl", out var returnUrlObj));
            Assert.Equal("/return", (string)returnUrlObj!);

            Assert.True(redirect.RouteValues.TryGetValue("RememberMe", out var rememberMeObj));
            Assert.True((bool)rememberMeObj!);
        }

        // Ensures locked-out accounts redirect to Lockout page and log a warning.
        [Fact]
        public async Task OnPostAsync_LockedOut_RedirectsToLockoutPage()
        {
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "wrong"
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            var result = await _loginModel.OnPostAsync();

            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./Lockout", redirect.PageName);

            _loggerMock.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null!,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        // Verifies invalid credentials add an error to ModelState and return the login page.
        [Fact]
        public async Task OnPostAsync_InvalidCredentials_AddsErrorAndReturnsPage()
        {
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "wrong"
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var result = await _loginModel.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(_loginModel.ModelState.IsValid);

            var error = Assert.Single(_loginModel.ModelState[string.Empty]!.Errors);
            Assert.Equal("Invalid login attempt.", error.ErrorMessage);
        }

        // Duplicate of OnPostAsync_RequiresTwoFactor_RedirectsTo2faPage; consider removing to reduce redundancy.
        [Fact]
        public async Task OnPostAsync_RequiresTwoFactor_RedirectsTo2fa()
        {
            _loginModel.Input = new LoginModel.InputModel { Email = "test@example.com", Password = "Password123!" };
            _signInManagerMock.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

            var result = await _loginModel.OnPostAsync("/return");

            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./LoginWith2fa", redirect.PageName);
        }

        #endregion
    }
}