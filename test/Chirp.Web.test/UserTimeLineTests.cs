// test/Chirp.Web.test/UserTimeLineTests.cs
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Core;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Web.Pages;
using Xunit;

namespace Tests.Web;

public class UserTimelineModelTests
{
    [Fact]
    public async Task OnGetAsync_MyOwnTimeline_ShowsPrivateTimeline()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();

        // When viewing own timeline ("alice" viewing "alice"), the service calls GetTimelineForUserAsync,
        // which returns both own cheeps and cheeps from followed authors.
        repoMock.Setup(r => r.GetTimelineForUserAsync("alice"))
                .ReturnsAsync(new List<MessageDTO>
                {
                    new() { AuthorName = "alice", Text = "My cheep", TimeStamp = DateTime.UtcNow },
                    new() { AuthorName = "bob", Text = "Followed cheep", TimeStamp = DateTime.UtcNow }
                });

        var service = new CheepService(repoMock.Object);

        var model = new UserTimelineModel(service, repoMock.Object)
        {
            // This assignment is redundant because it is already set in the constructor,
            // but it ensures the property is explicitly available for the test.
            CheepRepository = repoMock.Object,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Simulate that the current logged-in user is "alice"
        model.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "alice") }, "mock"));

        // Act
        await model.OnGetAsync("alice");

        // Assert
        // The page should be rendered for the requested author
        Assert.Equal("alice", model.Author);

        // Both own and followed cheeps should appear (private timeline behavior)
        Assert.Equal(2, model.Cheeps.Count);
        Assert.Contains(model.Cheeps, c => c.AuthorName == "bob");
    }

    [Fact]
    public async Task OnGetAsync_OtherUser_ShowsOnlyTheirCheeps()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();

        // When viewing another user's timeline (anyone viewing "bob"), only that user's own cheeps are shown.
        // The model uses GetAllCheepsFromAuthorAsync in this case (public view).
        repoMock.Setup(r => r.GetAllCheepsFromAuthorAsync("bob"))
                .ReturnsAsync(new List<MessageDTO>
                {
                    new() { AuthorName = "bob", Text = "Hi!", TimeStamp = DateTime.UtcNow }
                });

        var service = new CheepService(repoMock.Object);

        var model = new UserTimelineModel(service, repoMock.Object)
        {
            CheepRepository = repoMock.Object,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Note: No authenticated user is set here, simulating either a logged-out user
        // or a user different from "bob". The model treats this as a public view.

        // Act
        await model.OnGetAsync("bob");

        // Assert
        Assert.Equal("bob", model.Author);

        // Only the target author's cheeps are returned (no followed cheeps)
        Assert.Single(model.Cheeps);
        Assert.Equal("bob", model.Cheeps[0].AuthorName);
    }
}