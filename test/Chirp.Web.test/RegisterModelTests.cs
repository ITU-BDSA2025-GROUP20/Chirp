// test/Chirp.Web.test/RegisterModelTests.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
        // Mocks for the underlying user store interfaces – all backed by the same mock to allow As<> casting
        private readonly Mock<IUserStore<IdentityUser>> _userStoreMock;
        private readonly Mock<IUserEmailStore<IdentityUser>> _emailStoreMock;
        private readonly Mock<IUserPasswordStore<IdentityUser>> _passwordStoreMock;
        
        // Other dependencies that are injected into RegisterModel
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

        // Dummy IUrlHelper that returns fixed URLs – used because RegisterModel generates confirmation links
        private class DummyUrlHelper : IUrlHelper
        {
            public ActionContext ActionContext { get; } = new ActionContext(
                new DefaultHttpContext { Request = { Scheme = "https" } },
                new RouteData(),
                new ActionDescriptor());

            public string? Action(UrlActionContext actionContext) => "https://localhost/action";

            public string? RouteUrl(UrlRouteContext routeContext) => "https://localhost/route";

            // Critical for confirmation email tests – returns a predictable confirmation page URL
            public string? Page(string pageName, string? pageHandler, object? values, string? protocol, string? host, string? fragment)
                => "https://localhost/confirm";

            public string? Content(string? contentPath) => contentPath;

            public bool IsLocalUrl(string? url) => true;

            public string? Link(string? routeName, object? values) => "https://localhost/link";
        }

        // Builds a fully-configured RegisterModel with all ASP.NET Core Identity dependencies mocked
        private RegisterModel CreateRegisterModel(bool requireConfirmedAccount = false)
        {
            // Control whether account confirmation is required – changes the flow after successful registration
            var identityOptions = new IdentityOptions
            {
                SignIn = { RequireConfirmedAccount = requireConfirmedAccount }
            };
            var optionsAccessor = Options.Create(identityOptions);

            var passwordHasher = new PasswordHasher<IdentityUser>();

            // UserManager is constructed manually because the real one requires many services
            var userManager = new UserManager<IdentityUser>(
                _userStoreMock.Object,
                optionsAccessor,
                passwordHasher,
                Enumerable.Empty<IUserValidator<IdentityUser>>(),
                Enumerable.Empty<IPasswordValidator<IdentityUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,   // services – not used in these tests
                null!);  // logger  – not used in these tests

            // Fake token provider needed for email confirmation link generation
            var tokenProvider = new Mock<IUserTwoFactorTokenProvider<IdentityUser>>();
            tokenProvider.Setup(x => x.CanGenerateTwoFactorTokenAsync(userManager, It.IsAny<IdentityUser>()))
                         .ReturnsAsync(true);
            tokenProvider.Setup(x => x.GenerateAsync(It.IsAny<string>(), userManager, It.IsAny<IdentityUser>()))
                         .ReturnsAsync("fake-token");

            userManager.RegisterTokenProvider("Default", tokenProvider.Object);

            // HttpContext setup – required for SignInManager to sign in the user
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var authenticationServiceMock = new Mock<IAuthenticationService>();
            authenticationServiceMock.Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                                     .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(IAuthenticationService)))
                               .Returns(authenticationServiceMock.Object);

            httpContext.RequestServices = serviceProviderMock.Object;
            httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            // Remaining SignInManager dependencies
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
            claimsFactory.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>()))
                         .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity()));

            var schemesProvider = new Mock<IAuthenticationSchemeProvider>();
            schemesProvider.Setup(x => x.GetAllSchemesAsync())
                           .ReturnsAsync(new List<AuthenticationScheme>());

            var userConfirmation = new Mock<IUserConfirmation<IdentityUser>>();
            userConfirmation.Setup(x => x.IsConfirmedAsync(userManager, It.IsAny<IdentityUser>()))
                            .ReturnsAsync(true);

            var signInManager = new SignInManager<IdentityUser>(
                userManager,
                httpContextAccessor.Object,
                claimsFactory.Object,
                optionsAccessor,
                new Mock<ILogger<SignInManager<IdentityUser>>>().Object,
                schemesProvider.Object,
                userConfirmation.Object);

            // Create the page model with all required services
            var model = new RegisterModel(
                userManager,
                _userStoreMock.Object,
                signInManager,
                _loggerMock.Object,
                _emailSenderMock.Object);

            model.Url = new DummyUrlHelper();

            // TempData is used for status messages – mock prevents null reference
            var tempDataMock = new Mock<ITempDataDictionary>();
            model.TempData = tempDataMock.Object;

            // PageContext is required for some Razor Pages internals
            var pageContext = new PageContext
            {
                HttpContext = httpContext,
                ActionDescriptor = new CompiledPageActionDescriptor(),
                RouteData = new RouteData()
            };

            model.PageContext = pageContext;
            model.ModelState.Clear();

            return model;
        }

        [Fact]
        public async Task OnGetAsync_SetsReturnUrlAndExternalLogins()
        {
            var model = CreateRegisterModel();

            await model.OnGetAsync("/return-here");

            // ReturnUrl is stored for later redirection after registration
            Assert.Equal("/return-here", model.ReturnUrl);
            // ExternalLogins is populated by SignInManager – ensures it's not null
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

            // Invalid ModelState short-circuits the handler and redisplay the page
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

            // Setup successful user creation path
            _userStoreMock.Setup(x => x.SetUserNameAsync(It.IsAny<IdentityUser>(), "test@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _emailStoreMock.Setup(x => x.SetEmailAsync(It.IsAny<IdentityUser>(), "test@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _passwordStoreMock.Setup(x => x.SetPasswordHashAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userStoreMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Success));

            // Verify that a confirmation email is sent
            _emailSenderMock.Setup(x => x.SendEmailAsync("test@example.com", "Confirm your email", It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await model.OnPostAsync("/return");

            // When confirmation is required, user is redirected to the confirmation page
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

            // Same successful creation setup as above
            _userStoreMock.Setup(x => x.SetUserNameAsync(It.IsAny<IdentityUser>(), "test@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _emailStoreMock.Setup(x => x.SetEmailAsync(It.IsAny<IdentityUser>(), "test@example.com", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _passwordStoreMock.Setup(x => x.SetPasswordHashAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userStoreMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Success));

            var result = await model.OnPostAsync("/return");

            // When no confirmation is required, user is signed in and redirected to ReturnUrl
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

            // Successful property sets, but CreateAsync fails (e.g., duplicate email)
            _userStoreMock.Setup(x => x.SetUserNameAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _emailStoreMock.Setup(x => x.SetEmailAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _passwordStoreMock.Setup(x => x.SetPasswordHashAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userStoreMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Duplicate email" })));

            var result = await model.OnPostAsync();

            // Failed creation adds Identity errors to ModelState and redisplays the page
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

            // Successful creation setup
            _userStoreMock.Setup(x => x.SetUserNameAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _emailStoreMock.Setup(x => x.SetEmailAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _passwordStoreMock.Setup(x => x.SetPasswordHashAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userStoreMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Success));

            // Capture the email body to inspect the generated confirmation link text
            string? sentBody = null;
            _emailSenderMock
                .Setup(x => x.SendEmailAsync("test@example.com", "Confirm your email", It.IsAny<string>()))
                .Callback<string, string, string>((_, __, body) => sentBody = body)
                .Returns(Task.CompletedTask)
                .Verifiable();

            await model.OnPostAsync();

            _emailSenderMock.Verify();
            Assert.NotNull(sentBody);
            // The default Identity UI template uses "clicking here" for the link text
            Assert.Contains("clicking here", sentBody);
        }

        [Fact]
        public async Task OnPostAsync_PasswordMismatch_AddsError()
        {
            var model = CreateRegisterModel();

            model.Input = new RegisterModel.InputModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Different123!"
            };

            // The [Compare] attribute on ConfirmPassword is not automatically run in unit tests,
            // so we manually validate the InputModel to populate ModelState with the mismatch error
            var validationContext = new ValidationContext(model.Input);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(model.Input, validationContext, validationResults, true);

            foreach (var validationResult in validationResults)
            {
                model.ModelState.AddModelError("", validationResult.ErrorMessage ?? "Validation error");
            }

            var postResult = await model.OnPostAsync();

            // Mismatched passwords should prevent submission and redisplay the page
            Assert.IsType<PageResult>(postResult);
            Assert.False(model.ModelState.IsValid);

            var errors = model.ModelState["Input.ConfirmPassword"]?.Errors
                         ?? model.ModelState[string.Empty]?.Errors;

            Assert.NotNull(errors);
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.ErrorMessage.Contains("do not match", StringComparison.OrdinalIgnoreCase));
        }
    }
}