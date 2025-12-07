// test/Pages/UserTimelineModelTests.cs
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

namespace Tests.Pages;

public class UserTimelineModelTests
{
    [Fact]
    public async Task OnGetAsync_MyOwnTimeline_ShowsPrivateTimeline()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();
        repoMock.Setup(r => r.GetTimelineForUserAsync("alice"))
                .ReturnsAsync(new List<MessageDTO>
                {
                    new() { AuthorName = "alice", Text = "My cheep", TimeStamp = DateTime.UtcNow },
                    new() { AuthorName = "bob", Text = "Followed cheep", TimeStamp = DateTime.UtcNow }
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

        model.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "alice") }, "mock"));

        // Act
        await model.OnGetAsync("alice");

        // Assert
        Assert.Equal("alice", model.Author);
        Assert.Equal(2, model.Cheeps.Count);
        Assert.Contains(model.Cheeps, c => c.AuthorName == "bob");
    }

    [Fact]
    public async Task OnGetAsync_OtherUser_ShowsOnlyTheirCheeps()
    {
        // Arrange
        var repoMock = new Mock<ICheepRepository>();
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

        // Act
        await model.OnGetAsync("bob");

        // Assert
        Assert.Equal("bob", model.Author);
        Assert.Single(model.Cheeps);
        Assert.Equal("bob", model.Cheeps[0].AuthorName);
    }
}