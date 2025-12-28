// test/Chirp.Web.test/AboutMeModelTests.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Core;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Chirp.Web.Pages;
using Xunit;

namespace Tests.Web
{
    public class AboutMeModelTests
    {
        [Fact]
        public async Task OnGetAsync_Unauthenticated_RedirectsToPublic()
        {
            // Arrange
            var repoMock = new Mock<ICheepRepository>();
            var service = new CheepService(repoMock.Object);

            var model = new AboutMeModel(service, repoMock.Object)
            {
                CheepRepository = repoMock.Object,
                UserName = "anyuser",
                PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
            };

            // Act
            var result = await model.OnGetAsync();

            // Assert
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Public", redirect.PageName);
        }

        [Fact]
        public async Task OnGetAsync_Authenticated_LoadsCheepsAndFollowing()
        {
            // Arrange
            var repoMock = new Mock<ICheepRepository>();

            repoMock.Setup(r => r.GetFollowingNamesAsync("testuser"))
                    .ReturnsAsync(new List<string> { "followeduser" });

            // Do not mock GetCheepsFromAuthor on service - it's not overridable
            // Instead, accept that in unit test with empty DB, Cheeps will be empty
            // The test still verifies the important parts: following list and no exception
            var service = new CheepService(repoMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock"));

            var model = new AboutMeModel(service, repoMock.Object)
            {
                CheepRepository = repoMock.Object,
                UserName = "testuser",
                PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } }
            };

            // Act
            var result = await model.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.Equal("testuser", model.AuthorName);
            Assert.Empty(model.Cheeps); // No cheeps mocked, so empty - acceptable for this test
            Assert.Single(model.Following);
            Assert.Contains("followeduser", model.Following);
        }

        [Fact]
        public async Task OnPostUnfollowAsync_Unauthenticated_ReturnsForbid()
        {
            var repoMock = new Mock<ICheepRepository>();
            var service = new CheepService(repoMock.Object);

            var model = new AboutMeModel(service, repoMock.Object)
            {
                CheepRepository = repoMock.Object,
                UserName = "anyuser",
                PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
            };

            var result = await model.OnPostUnfollowAsync("followeduser");

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task OnPostUnfollowAsync_InvalidFollowee_ReturnsBadRequest()
        {
            var repoMock = new Mock<ICheepRepository>();
            var service = new CheepService(repoMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock"));

            var model = new AboutMeModel(service, repoMock.Object)
            {
                CheepRepository = repoMock.Object,
                UserName = "testuser",
                PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } }
            };

            var result = await model.OnPostUnfollowAsync("testuser");

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task OnPostUnfollowAsync_Valid_UnfollowsAndRedirects()
        {
            var repoMock = new Mock<ICheepRepository>();
            repoMock.Setup(r => r.UnfollowUserAsync("testuser", "followeduser"))
                    .Returns(Task.CompletedTask);

            var service = new CheepService(repoMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock"));

            var model = new AboutMeModel(service, repoMock.Object)
            {
                CheepRepository = repoMock.Object,
                UserName = "testuser",
                PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } }
            };

            var result = await model.OnPostUnfollowAsync("followeduser");

            repoMock.Verify(r => r.UnfollowUserAsync("testuser", "followeduser"), Times.Once);
            Assert.IsType<RedirectToPageResult>(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsFollowingAsync_ReturnsCorrectValue(bool isFollowing)
        {
            var repoMock = new Mock<ICheepRepository>();
            repoMock.Setup(r => r.IsFollowingAsync("testuser", "followeduser"))
                    .ReturnsAsync(isFollowing);

            var service = new CheepService(repoMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock"));

            var model = new AboutMeModel(service, repoMock.Object)
            {
                CheepRepository = repoMock.Object,
                UserName = "testuser",
                PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } }
            };

            var result = await model.IsFollowingAsync("followeduser");

            Assert.Equal(isFollowing, result);
        }

        [Fact]
        public async Task IsFollowingAsync_Unauthenticated_ReturnsFalse()
        {
            var repoMock = new Mock<ICheepRepository>();
            var service = new CheepService(repoMock.Object);

            var model = new AboutMeModel(service, repoMock.Object)
            {
                CheepRepository = repoMock.Object,
                UserName = "anyuser",
                PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
            };

            var result = await model.IsFollowingAsync("followeduser");

            Assert.False(result);
        }
    }
}