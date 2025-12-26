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
        repoMock.Setup(r => r.GetAllCheepsAsync())
                .ReturnsAsync(new List<MessageDTO>
                {
                    new() { AuthorName = "Jacqueline", Text = "Hello!", TimeStamp = DateTime.UtcNow }
                });

        var service = new CheepService(repoMock.Object); // Real service!

        var model = new PublicModel(service, repoMock.Object)
        {
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
        repoMock.Setup(r => r.StoreCheepAsync(Capture.In(capture)))
                .Returns(Task.CompletedTask);

        var service = new CheepService(repoMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "oskar") }, "mock"));

        var model = new PublicModel(service, repoMock.Object)
        {
            CheepRepository = repoMock.Object,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = user } },
            Text = "Hello from test!"
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Single(capture);
        Assert.Equal("Hello from test!", capture[0].Text);
        Assert.Equal("oskar", capture[0].AuthorName);
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
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext() // no user
            },
            Text = "irrelevant"
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}