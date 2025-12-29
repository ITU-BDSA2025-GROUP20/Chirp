// test/Chirp.Web.test/PublicModelTests.cs
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
using Web.Pages;
using Xunit;

namespace Tests.Web;

public class PublicModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsCheeps_FromService()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();
        
        // Sets up the repository mock to return a single cheep when GetAllCheepsAsync is called
        repoMock.Setup(r => r.GetAllCheepsAsync())
                .ReturnsAsync(new List<MessageDTO>
                {
                    new() { AuthorName = "Jacqueline", Text = "Hello!", TimeStamp = DateTime.UtcNow }
                });

        // Uses a real CheepService instance (not mocked) to ensure service logic is exercised
        var service = new CheepService(repoMock.Object);

        var model = new PublicModel(service, repoMock.Object)
        {
            // Explicitly set for clarity, though the constructor likely assigns it
            CheepRepository = repoMock.Object,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.NotEmpty(model.Cheeps);
        Assert.Equal("Jacqueline", model.Cheeps[0].AuthorName);
    }

    [Fact]
    public async Task OnPostAsync_WhenAuthenticatedAndValid_StoresCheep_AndRedirects()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();
        var capture = new List<MessageDTO>();
        
        // Captures the MessageDTO passed to StoreCheepAsync for later inspection
        repoMock.Setup(r => r.StoreCheepAsync(Capture.In(capture)))
                .Returns(Task.CompletedTask);

        var service = new CheepService(repoMock.Object);

        // Simulates an authenticated user with name "oskar"
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "oskar") }, "mock"));

        var model = new PublicModel(service, repoMock.Object)
        {
            CheepRepository = repoMock.Object,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } },
            Text = "Hello from test!" // The cheep text submitted via the form
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Single(capture);
        Assert.Equal("Hello from test!", capture[0].Text);
        Assert.Equal("oskar", capture[0].AuthorName); // Author name is derived from the authenticated user
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_WhenNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();
        var service = new CheepService(repoMock.Object);

        var model = new PublicModel(service, repoMock.Object)
        {
            CheepRepository = repoMock.Object,
            // HttpContext has no User set → unauthenticated
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            },
            Text = "irrelevant"
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        // Posting a cheep requires authentication; unauthenticated users are forbidden
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnGetFollowAsync_Unauthenticated_ReturnsForbid()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();
        var service = new CheepService(repoMock.Object);
        var model = new PublicModel(service, repoMock.Object)
        {
            CheepRepository = repoMock.Object,
            // No authenticated user
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
        var result = await model.OnGetFollowAsync("followee");

        // Assert
        // Following a user requires authentication
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnGetFollowAsync_Authenticated_FollowsAndRedirects()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();
        
        // Expect FollowUserAsync to be called with the current user and the target followee
        repoMock.Setup(r => r.FollowUserAsync("testuser", "followee")).Returns(Task.CompletedTask);
        
        var service = new CheepService(repoMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock"));
        var model = new PublicModel(service, repoMock.Object)
        {
            CheepRepository = repoMock.Object,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } }
        };

        // Act
        var result = await model.OnGetFollowAsync("followee");

        // Assert
        repoMock.Verify(r => r.FollowUserAsync("testuser", "followee"), Times.Once);
        Assert.IsType<RedirectToPageResult>(result); // Redirects back to the public timeline
    }

    [Fact]
    public async Task OnGetUnfollowAsync_Authenticated_UnfollowsAndRedirects()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();
        repoMock.Setup(r => r.UnfollowUserAsync("testuser", "followee")).Returns(Task.CompletedTask);
        
        var service = new CheepService(repoMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock"));
        var model = new PublicModel(service, repoMock.Object)
        {
            CheepRepository = repoMock.Object,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } }
        };

        // Act
        var result = await model.OnGetUnfollowAsync("followee");

        // Assert
        repoMock.Verify(r => r.UnfollowUserAsync("testuser", "followee"), Times.Once);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_InvalidModel_ReturnsPageWithErrors()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();
        var service = new CheepService(repoMock.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock"));
        var model = new PublicModel(service, repoMock.Object)
        {
            CheepRepository = repoMock.Object,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } },
            // Text exceeds the 160-character limit enforced by model validation
            Text = new string('a', 161)
        };
        
        // Manually add a validation error to simulate failed model state (common in unit tests)
        model.ModelState.AddModelError(nameof(PublicModel.Text), "Cheeps cannot be longer than 160 characters.");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        // When model validation fails, the page is re-rendered with errors instead of redirecting
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }
}