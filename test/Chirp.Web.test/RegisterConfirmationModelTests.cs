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

        private static Mock<UserManager<IdentityUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var mock = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            return mock;
        }

        private void SetupPageContext(string scheme = "https")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = scheme;

            // Set the current page to something in the Identity area so routing can "find" pages
            var routeData = new RouteData();
            routeData.Values["page"] = "/Account/RegisterConfirmation";
            routeData.Values["area"] = "Identity";

            var actionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor
            {
                RouteValues = new System.Collections.Generic.Dictionary<string, string>
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

            // Always return a non-null URL so the code doesn't skip the assignment
            urlHelperMock
                .Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Returns<UrlRouteContext>(context =>
                {
                    var protocol = context.Protocol ?? httpContext.Request.Scheme;

                    var values = new RouteValueDictionary(context.Values ?? new { });

                    var area = values["area"]?.ToString() ?? "Identity";
                    var userId = values["userId"]?.ToString() ?? "";
                    var code = values["code"]?.ToString() ?? "";
                    var returnUrl = Uri.EscapeDataString(values["returnUrl"]?.ToString() ?? "");

                    return $"{protocol}://example.com/{area}/Account/ConfirmEmail?userId={userId}&code={code}&returnUrl={returnUrl}";
                });

            urlHelperMock
                .Setup(u => u.Content(It.IsAny<string>()))
                .Returns<string>(c => c ?? "/");

            _model.Url = urlHelperMock.Object;
        }

        [Fact]
        public async Task OnGetAsync_EmailNull_RedirectsToIndex()
        {
            SetupPageContext();

            var result = await _model.OnGetAsync(email: null);

            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Index", redirectResult.PageName);
        }

        [Fact]
        public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
        {
            SetupPageContext();
            const string email = "test@example.com";

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(email))
                .ReturnsAsync((IdentityUser)null);

            var result = await _model.OnGetAsync(email);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"Unable to load user with email '{email}'.", notFoundResult.Value);
        }

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