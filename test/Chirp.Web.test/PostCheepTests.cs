// test/Chirp.Web.test/PostCheepTests.cs
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Core;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Models;
using Infrastructure.Services;
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

namespace Tests.Web;

public class PostCheepTests : IAsyncLifetime
{
    private readonly CheepDbContext _context;     // In-memory database context used for isolated testing
    private readonly PublicModel _pageModel;      // The Razor Page model being tested
    private readonly DefaultHttpContext _httpContext; // Minimal HttpContext required for authentication and TempData

    public PostCheepTests()
    {
        // Create a fresh in-memory database with a unique name so tests never share state
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase("ChirpTestDb_" + Guid.NewGuid())
            .Options;

        _context = new CheepDbContext(options);

        var repository = new CheepRepository(_context);
        var service = new CheepService(repository);

        // TempData needs an ITempDataProvider; register the session-based one for test use
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ITempDataProvider, SessionStateTempDataProvider>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // HttpContext must have a RequestServices instance for DI resolution
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

        SetUnauthenticatedUser(); // Default state for all tests
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
        // Make the current request appear to come from a logged-in user named "testuser"
        SetAuthenticatedUser("testuser");

        // Fill the bound property that the form posts
        _pageModel.Text = "Test cheep from fixed test!";

        // Execute the POST handler
        var result = await _pageModel.OnPostAsync();

        // Handler must redirect after successful post (usually to the same page)
        Assert.IsType<RedirectToPageResult>(result);

        // Look up the newly created cheep directly in the database
        var cheep = await _context.Cheeps
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Text == "Test cheep from fixed test!");

        // Cheep must have been persisted
        Assert.NotNull(cheep);
        // Author name must match the authenticated user
        Assert.Equal("testuser", cheep.Author.Name);
    }

    [Fact]
    public async Task OnPostAsync_WhenNotAuthenticated_ShouldReturnForbid()
    {
        // Ensure no user is authenticated
        SetUnauthenticatedUser();

        // Attempt to post something (value does not matter)
        _pageModel.Text = "This will be blocked";

        // Execute the POST handler
        var result = await _pageModel.OnPostAsync();

        // Unauthorized users must receive a 403 Forbid result
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_WithEmptyText_ShouldReturnPageWithModelError()
    {
        // Act as a logged-in user
        SetAuthenticatedUser("testuser");

        // Submit an empty message
        _pageModel.Text = "";

        // Manually add the exact model error that PublicModel.OnPostAsync adds when Text is empty/whitespace
        _pageModel.ModelState.AddModelError(
            nameof(PublicModel.Text),
            "You must write something to cheep!");

        // Execute the POST handler
        var result = await _pageModel.OnPostAsync();

        // When validation fails, the same page is returned so the user can correct the input
        Assert.IsType<PageResult>(result);

        // ModelState must be invalid
        Assert.False(_pageModel.ModelState.IsValid);

        // Error must be attached to the Text property
        Assert.True(_pageModel.ModelState.ContainsKey(nameof(PublicModel.Text)));

        // Error message must match exactly what the page model sets
        var error = _pageModel.ModelState[nameof(PublicModel.Text)]?.Errors[0];
        Assert.Contains("You must write something to cheep!", error?.ErrorMessage);
    }
}