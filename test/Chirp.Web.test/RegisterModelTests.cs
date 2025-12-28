// test/Chirp.Web.test/RegisterModelTests.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Areas.Identity.Pages.Account;

namespace Tests.Web
{
    public class RegisterModelTests
    {
        private readonly Mock<IUserStore<IdentityUser>> _userStoreMock;
        private readonly Mock<IUserEmailStore<IdentityUser>> _emailStoreMock;
        private readonly Mock<IUserPasswordStore<IdentityUser>> _passwordStoreMock;
        private readonly Mock<ILogger<RegisterModel>> _loggerMock;
        private readonly Mock<IEmailSender> _emailSenderMock;

        public RegisterModelTests()
        {
            _userStoreMock = new Mock<IUserStore<IdentityUser>>(MockBehavior.Loose);
            _emailStoreMock = _userStoreMock.As<IUserEmailStore<IdentityUser>>();
            _passwordStoreMock = _userStoreMock.As<IUserPasswordStore<IdentityUser>>();

            _loggerMock = new Mock<ILogger<RegisterModel>>();
            _emailSenderMock = new Mock<IEmailSender>();
        }

        private class DummyUrlHelper : IUrlHelper
        {
            public ActionContext ActionContext { get; } = new ActionContext(
                new DefaultHttpContext { Request = { Scheme = "https" } },
                new RouteData(),
                new ActionDescriptor());

            public string? Action(UrlActionContext actionContext) => "https://localhost/action";

            public string? RouteUrl(UrlRouteContext routeContext) => "https://localhost/route";

            public string? Page(string pageName, string? pageHandler, object? values, string? protocol, string? host, string? fragment)
                => "https://localhost/confirm";

            public string? Content(string contentPath) => contentPath;

            public bool IsLocalUrl(string? url) => true;

            public string? Link(string? routeName, object? values) => "https://localhost/link";
        }

        private RegisterModel CreateRegisterModel(bool requireConfirmedAccount = false)
        {
            var identityOptions = new IdentityOptions
            {
                SignIn = { RequireConfirmedAccount = requireConfirmedAccount }
            };
            var optionsAccessor = Options.Create(identityOptions);

            var passwordHasher = new PasswordHasher<IdentityUser>();

            var userManager = new UserManager<IdentityUser>(
                _userStoreMock.Object,
                optionsAccessor,
                passwordHasher,
                null, null, null, null, null, null);

            // Enable email support
            var supportsEmailProp = typeof(UserManager<IdentityUser>)
                .GetProperty("SupportsUserEmail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            supportsEmailProp?.SetValue(userManager, true);

            // Register default token provider
            var tokenProviderMock = new Mock<IUserTwoFactorTokenProvider<IdentityUser>>();
            tokenProviderMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), userManager, It.IsAny<IdentityUser>()))
                .ReturnsAsync("raw-token");

            var tokenProvidersField = typeof(UserManager<IdentityUser>)
                .GetField("_tokenProviders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tokenProvidersDict = (Dictionary<string, IUserTwoFactorTokenProvider<IdentityUser>>)tokenProvidersField!.GetValue(userManager)!;
            tokenProvidersDict["Default"] = tokenProviderMock.Object;

            // Create dependencies for SignInManager
            var httpContext = new DefaultHttpContext();
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
            claimsFactory.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync((IdentityUser u) => new ClaimsPrincipal(new ClaimsIdentity()));

            var schemesProvider = new Mock<IAuthenticationSchemeProvider>();
            schemesProvider.Setup(x => x.GetAllSchemesAsync())
                .ReturnsAsync(new List<AuthenticationScheme>());

            // Create SignInManager mock AFTER userManager exists
            var signInManagerMock = new Mock<SignInManager<IdentityUser>>(
                userManager,
                httpContextAccessor.Object,
                claimsFactory.Object,
                null,
                null,
                schemesProvider.Object,
                null);

            var model = new RegisterModel(
                userManager,
                _userStoreMock.Object,
                signInManagerMock.Object,
                _loggerMock.Object,
                _emailSenderMock.Object);

            model.ModelState.Clear();

            model.Url = new DummyUrlHelper();

            var pageHttpContext = new DefaultHttpContext();
            pageHttpContext.Request.Scheme = "https";

            model.PageContext = new PageContext
            {
                HttpContext = pageHttpContext
            };

            return model;
        }

        [Fact]
        public async Task OnGetAsync_SetsReturnUrlAndExternalLogins()
        {
            var model = CreateRegisterModel();

            await model.OnGetAsync("/return-here");

            Assert.Equal("/return-here", model.ReturnUrl);
            Assert.NotNull(model.ExternalLogins);
        }

        [Fact]
        public async Task OnPostAsync_InvalidModelState_ReturnsPage()
        {
            var model = CreateRegisterModel();
            model.ModelState.AddModelError("Email", "Required");

            model.Input = new RegisterModel.InputModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_UserCreationSucceeds_RequireConfirmedAccount_RedirectsToConfirmation()
        {
            var model = CreateRegisterModel(requireConfirmedAccount: true);

            model.Input = new RegisterModel.InputModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _userStoreMock.Setup(x => x.SetUserNameAsync(It.IsAny<IdentityUser>(), "test@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _emailStoreMock.Setup(x => x.SetEmailAsync(It.IsAny<IdentityUser>(), "test@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _passwordStoreMock.Setup(x => x.SetPasswordHashAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userStoreMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Success));

            _emailSenderMock.Setup(x => x.SendEmailAsync("test@example.com", "Confirm your email", It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await model.OnPostAsync("/return");

            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("RegisterConfirmation", redirect.PageName);
            Assert.Equal("test@example.com", redirect.RouteValues!["email"]);

            _emailSenderMock.Verify();
        }

        [Fact]
        public async Task OnPostAsync_UserCreationSucceeds_NoConfirmationRequired_SignsInAndRedirects()
        {
            var model = CreateRegisterModel(requireConfirmedAccount: false);

            model.Input = new RegisterModel.InputModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _userStoreMock.Setup(x => x.SetUserNameAsync(It.IsAny<IdentityUser>(), "test@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _emailStoreMock.Setup(x => x.SetEmailAsync(It.IsAny<IdentityUser>(), "test@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _passwordStoreMock.Setup(x => x.SetPasswordHashAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userStoreMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Success));

            var result = await model.OnPostAsync("/return");

            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/return", redirect.Url);
        }

        [Fact]
        public async Task OnPostAsync_UserCreationFails_AddsErrorsAndReturnsPage()
        {
            var model = CreateRegisterModel();

            model.Input = new RegisterModel.InputModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _userStoreMock.Setup(x => x.SetUserNameAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _emailStoreMock.Setup(x => x.SetEmailAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _passwordStoreMock.Setup(x => x.SetPasswordHashAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userStoreMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Duplicate email" })));

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.Contains(model.ModelState[string.Empty]!.Errors, e => e.ErrorMessage == "Duplicate email");
        }

        [Fact]
        public async Task OnPostAsync_SendsConfirmationEmailWithCorrectLink()
        {
            var model = CreateRegisterModel(requireConfirmedAccount: true);

            model.Input = new RegisterModel.InputModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _userStoreMock.Setup(x => x.SetUserNameAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _emailStoreMock.Setup(x => x.SetEmailAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _passwordStoreMock.Setup(x => x.SetPasswordHashAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userStoreMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Success));

            string? sentBody = null;
            _emailSenderMock
                .Setup(x => x.SendEmailAsync("test@example.com", "Confirm your email", It.IsAny<string>()))
                .Callback<string, string, string>((_, __, body) => sentBody = body)
                .Returns(Task.CompletedTask)
                .Verifiable();

            await model.OnPostAsync();

            _emailSenderMock.Verify();
            Assert.NotNull(sentBody);
            Assert.Contains("clicking here", sentBody);
        }
    }
}