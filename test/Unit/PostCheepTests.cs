// test/Unit/PostCheepTests.cs
using System.Security.Claims;
using System.Threading.Tasks;
using Core;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Models;
using Web.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Web.Tests;

public class PostCheepTests : IAsyncLifetime
{
    private readonly CheepDbContext _context;
    private readonly PublicModel _pageModel;
    private readonly DefaultHttpContext _httpContext;

    public PostCheepTests()
    {
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase("ChirpTestDb_" + Guid.NewGuid())
            .Options;

        _context = new CheepDbContext(options);

        var repository = new CheepRepository(_context);
        var service = new Infrastructure.Services.CheepService(repository);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ITempDataProvider, SessionStateTempDataProvider>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var pageContext = new PageContext
        {
            HttpContext = _httpContext
        };

        _pageModel = new PublicModel(service, repository)
        {
            CheepRepository = repository,
            PageContext = pageContext,
            TempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>())
        };

        SetUnauthenticatedUser();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _context.DisposeAsync();

    private void SetAuthenticatedUser(string name)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, name)
        }, "TestAuth"); 

        var principal = new ClaimsPrincipal(identity);

        _httpContext.User = principal;
        _pageModel.PageContext.HttpContext.User = principal;
   
    }

    private void SetUnauthenticatedUser()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        _httpContext.User = anonymous;
        _pageModel.PageContext.HttpContext.User = anonymous;
    }

    [Fact]
    public async Task OnPostAsync_WhenAuthenticated_ShouldStoreCheepAndRedirect()
    {
     
        SetAuthenticatedUser("testuser");
        _pageModel.Text = "Test cheep from fixed test!";

        
        var result = await _pageModel.OnPostAsync();

      
        Assert.IsType<RedirectToPageResult>(result);

        var cheep = await _context.Cheeps
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Text == "Test cheep from fixed test!");

        Assert.NotNull(cheep);
        Assert.Equal("testuser", cheep.Author.Name);
    }

    [Fact]
    public async Task OnPostAsync_WhenNotAuthenticated_ShouldReturnForbid()
    {
     
        SetUnauthenticatedUser();
        _pageModel.Text = "This will be blocked";

      
        var result = await _pageModel.OnPostAsync();

     
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_WithEmptyText_ShouldReturnPageWithModelError()
    {
        
        SetAuthenticatedUser("testuser");
        _pageModel.Text = ""; 

   
        _pageModel.ModelState.AddModelError(
            nameof(PublicModel.Text), 
            "You must write something to cheep!");

     
        var result = await _pageModel.OnPostAsync();

        
        Assert.IsType<PageResult>(result);
        Assert.False(_pageModel.ModelState.IsValid);
        Assert.True(_pageModel.ModelState.ContainsKey(nameof(PublicModel.Text)));
        var error = _pageModel.ModelState[nameof(PublicModel.Text)]?.Errors[0];
        Assert.Contains("You must write something to cheep!", error?.ErrorMessage);
    }
}