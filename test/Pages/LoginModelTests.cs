// test/Pages/LoginModelTests.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Pages
{
    public class LoginModelTests
    {
        private readonly Mock<SignInManager<IdentityUser>> _signInManagerMock;
        private readonly Mock<ILogger<LoginModel>> _loggerMock;
        private readonly Mock<IUrlHelper> _urlHelperMock;
        private readonly Mock<ITempDataDictionary> _tempDataMock;
        private readonly LoginModel _loginModel;

        public LoginModelTests()
        {
            // Mock UserManager (required for SignInManager)
            var userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);

            // Mock SignInManager
            _signInManagerMock = new Mock<SignInManager<IdentityUser>>(
                userManagerMock.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(),
                null, null, null, null);

            _loggerMock = new Mock<ILogger<LoginModel>>();

            // Mock IUrlHelper – returns "/" for Url.Content("~/")
            _urlHelperMock = new Mock<IUrlHelper>();
            _urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

            // Mock ITempDataDictionary
            _tempDataMock = new Mock<ITempDataDictionary>();

            // Create the page model
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

        private void SetupAuthenticationServiceMock()
        {
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, null))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            _loginModel.PageContext.HttpContext.RequestServices = serviceProviderMock.Object;
        }

        #region OnGetAsync Tests

        [Fact]
        public async Task OnGetAsync_WithErrorMessage_AddsModelError()
        {
            // Arrange
            SetupAuthenticationServiceMock();
            _loginModel.ErrorMessage = "Test error";

            // Act
            await _loginModel.OnGetAsync();

            // Assert
            Assert.False(_loginModel.ModelState.IsValid);
            var error = _loginModel.ModelState[string.Empty].Errors[0];
            Assert.Equal("Test error", error.ErrorMessage);
        }

        [Fact]
        public async Task OnGetAsync_DefaultsReturnUrlToHome()
        {
            // Arrange
            SetupAuthenticationServiceMock();

            // Act
            await _loginModel.OnGetAsync();

            // Assert
            Assert.Equal("/", _loginModel.ReturnUrl);
        }

        [Fact]
        public async Task OnGetAsync_UsesProvidedReturnUrl()
        {
            // Arrange
            SetupAuthenticationServiceMock();

            // Act
            await _loginModel.OnGetAsync("/custom-return");

            // Assert
            Assert.Equal("/custom-return", _loginModel.ReturnUrl);
        }

        [Fact]
        public async Task OnGetAsync_SignsOutExternalScheme()
        {
            // Arrange
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, null))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            _loginModel.PageContext.HttpContext.RequestServices = serviceProviderMock.Object;

            // Act
            await _loginModel.OnGetAsync();

            // Assert
            authServiceMock.Verify();
        }

        [Fact]
        public async Task OnGetAsync_LoadsExternalLogins()
        {
            // Arrange
            SetupAuthenticationServiceMock();

            var schemes = new List<AuthenticationScheme>
            {
                new AuthenticationScheme("Google", "Google", typeof(AuthenticationHandler<AuthenticationSchemeOptions>))
            };

            _signInManagerMock
                .Setup(s => s.GetExternalAuthenticationSchemesAsync())
                .ReturnsAsync(schemes);

            // Act
            await _loginModel.OnGetAsync();

            // Assert
            Assert.Equal(schemes, _loginModel.ExternalLogins);
        }

        #endregion

        #region OnPostAsync Tests

        [Fact]
        public async Task OnPostAsync_InvalidModelState_ReturnsPage()
        {
            // Arrange
            _loginModel.ModelState.AddModelError("Test", "Invalid");

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_SuccessfulLogin_RedirectsToReturnUrl()
        {
            // Arrange
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "password",
                RememberMe = true
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync("test@example.com", "password", true, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _loginModel.OnPostAsync("/custom");

            // Assert
            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/custom", redirect.Url);

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task OnPostAsync_DefaultReturnUrlOnSuccess()
        {
            // Arrange
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "password"
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/", redirect.Url);
        }

        [Fact]
        public async Task OnPostAsync_RequiresTwoFactor_RedirectsTo2faPage()
        {
            // Arrange
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "password",
                RememberMe = true
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), true, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

            // Act
            var result = await _loginModel.OnPostAsync("/return");

            // Assert
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./LoginWith2fa", redirect.PageName);
            Assert.Equal("/return", redirect.RouteValues["ReturnUrl"]);
            Assert.True((bool)redirect.RouteValues["RememberMe"]);
        }

        [Fact]
        public async Task OnPostAsync_LockedOut_RedirectsToLockoutPage()
        {
            // Arrange
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "wrong"
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./Lockout", redirect.PageName);

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task OnPostAsync_InvalidCredentials_AddsErrorAndReturnsPage()
        {
            // Arrange
            _loginModel.Input = new LoginModel.InputModel
            {
                Email = "test@example.com",
                Password = "wrong"
            };

            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _loginModel.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(_loginModel.ModelState.IsValid);
            var error = _loginModel.ModelState[string.Empty].Errors[0];
            Assert.Equal("Invalid login attempt.", error.ErrorMessage);
        }

        #endregion
    }
}