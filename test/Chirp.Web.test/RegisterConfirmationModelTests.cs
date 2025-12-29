// test/Chirp.Web.test/RegisterConfirmationModelTests.cs
using System;
using System.Text;
using System.Threading.Tasks;
using Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using Xunit;

namespace Tests.Web
{
    public class RegisterConfirmationModelTests
    {
        private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly RegisterConfirmationModel _model;

        public RegisterConfirmationModelTests()
        {
            _userManagerMock = GetMockUserManager();
            _emailSenderMock = new Mock<IEmailSender>();
            _model = new RegisterConfirmationModel(_userManagerMock.Object, _emailSenderMock.Object);
        }

        // Creates a minimal UserManager mock. The many null! parameters are the optional services
        // (options, password hasher, user validators, etc.) that are not needed for these tests.
        private static Mock<UserManager<IdentityUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var mock = new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            return mock;
        }

        // Configures PageContext and a mocked IUrlHelper so that the page model can generate URLs.
        // The URL helper is set up to produce predictable confirmation URLs like:
        // https://example.com/Identity/Account/ConfirmEmail?userId=...&code=...&returnUrl=...
        private void SetupPageContext(string scheme = "https")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = scheme;

            // RouteData and ActionDescriptor tell the framework that the current page is
            // /Account/RegisterConfirmation in the Identity area – required for correct routing.
            var routeData = new RouteData();
            routeData.Values["page"] = "/Account/RegisterConfirmation";
            routeData.Values["area"] = "Identity";

            var actionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor
            {
                RouteValues = new System.Collections.Generic.Dictionary<string, string?>
                {
                    { "page", "/Account/RegisterConfirmation" },
                    { "area", "Identity" }
                }
            };

            var actionContext = new ActionContext(
                httpContext,
                routeData,
                actionDescriptor);

            _model.PageContext = new PageContext(actionContext);

            var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Loose);

            urlHelperMock.SetupGet(u => u.ActionContext).Returns(actionContext);

            // Generates a deterministic confirmation URL based on the values passed to RouteUrl.
            // This prevents null URLs and allows the page model to assign EmailConfirmationUrl
            // when the feature is enabled.
            urlHelperMock
                .Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Returns<UrlRouteContext>(context =>
                {
                    var protocol = context.Protocol ?? httpContext.Request.Scheme;

                    var values = context.Values is not null 
                        ? new RouteValueDictionary(context.Values) 
                        : new RouteValueDictionary();

                    var area = values["area"]?.ToString() ?? "Identity";
                    var userId = values["userId"]?.ToString() ?? "";
                    var code = values["code"]?.ToString() ?? "";
                    var returnUrl = Uri.EscapeDataString(values["returnUrl"]?.ToString() ?? "");

                    return $"{protocol}://example.com/{area}/Account/ConfirmEmail?userId={userId}&code={code}&returnUrl={returnUrl}";
                });

            // Simple content path resolution – not used in these tests but required by the contract.
            urlHelperMock
                .Setup(u => u.Content(It.IsAny<string>()))
                .Returns<string>(c => c ?? "/");

            _model.Url = urlHelperMock.Object;
        }

        // If no email is provided, the scaffolded page immediately redirects to the home page (/Index).
        [Fact]
        public async Task OnGetAsync_EmailNull_RedirectsToIndex()
        {
            SetupPageContext();

            var result = await _model.OnGetAsync(email: null);

            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Index", redirectResult.PageName);
        }

        // When a valid-looking email is supplied but no user exists with that email,
        // the page returns a 404 NotFound with a specific message.
        [Fact]
        public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
        {
            SetupPageContext();
            const string email = "test@example.com";

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(email))
                .ReturnsAsync((IdentityUser?)null);

            var result = await _model.OnGetAsync(email);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"Unable to load user with email '{email}'.", notFoundResult.Value);
        }

        // For a valid user, the page renders normally and sets the Email property.
        // In the default scaffolded code, DisplayConfirmAccountLink is hardcoded to false,
        // so no confirmation link is generated or displayed.
        [Fact]
        public async Task OnGetAsync_ValidEmail_SetsPropertiesCorrectly()
        {
            SetupPageContext();
            const string email = "test@example.com";
            var user = new IdentityUser { UserName = email, Email = email };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(email))
                .ReturnsAsync(user);

            var result = await _model.OnGetAsync(email, returnUrl: "/custom-return");

            Assert.IsType<PageResult>(result);
            Assert.Equal(email, _model.Email);
            Assert.False(_model.DisplayConfirmAccountLink);
            Assert.Null(_model.EmailConfirmationUrl);
        }

        // Same scenario as above but with HTTP scheme and a different returnUrl.
        // Confirms that the confirmation link remains hidden by default in the scaffolded implementation.
        [Fact]
        public async Task OnGetAsync_ValidEmail_ByDefault_HidesConfirmationLink()
        {
            SetupPageContext(scheme: "http");
            const string email = "test@example.com";
            var user = new IdentityUser { UserName = email, Email = email };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(email))
                .ReturnsAsync(user);

            var result = await _model.OnGetAsync(email, returnUrl: "/my-return");

            Assert.IsType<PageResult>(result);
            Assert.Equal(email, _model.Email);
            Assert.False(_model.DisplayConfirmAccountLink);  // Hardcoded false in scaffolded code
            Assert.Null(_model.EmailConfirmationUrl);        // Not generated when link is hidden
        }
    }
}