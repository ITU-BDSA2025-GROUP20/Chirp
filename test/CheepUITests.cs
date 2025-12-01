using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace Web.Tests;

public class CheepUITests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

  public CheepUITests(WebApplicationFactory<Program> factory)
{
    _factory = factory;
}

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();

        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
            // Headless = false, SlowMo = 2000 → use for debugging
        });

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = client.BaseAddress!.ToString()
        });

        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    private async Task LoginAsync()
    {
        await _page!.GotoAsync("/");
        await _page.ClickAsync("text=login");
        await _page.FillAsync("input[name=\"Input.Email\"]", "ropf@itu.dk");
        await _page.FillAsync("input[name=\"Input.Password\"]", "LetM31n!");
        await _page.ClickAsync("button:has-text('Log in')");
        await Expect(_page.Locator("text=my timeline")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CheepBox_NotVisible_WhenNotLoggedIn()
    {
        await _page!.GotoAsync("/");
        await Expect(_page.Locator("textarea[name=\"Text\"]")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task CheepBox_Visible_WhenLoggedIn()
    {
        await LoginAsync();
        await Expect(_page.Locator("textarea[name=\"Text\"]")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CannotPostCheep_LongerThan160Characters()
    {
        await LoginAsync();

        var longText = new string('A', 161);
        await _page!.FillAsync("textarea[name=\"Text\"]", longText);
        await _page.ClickAsync("button[type=\"submit\"]");

        await Expect(_page.Locator("span.field-validation-error"))
            .ToContainTextAsync("Cheeps cannot be longer than 160 characters.");
    }

    [Fact]
    public async Task CanPostValidCheep_AndItAppears()
    {
        await LoginAsync();

        var cheepText = $"Playwright test cheep {DateTime.Now:HH:mm:ss}";
        await _page!.FillAsync("textarea[name=\"Text\"]", cheepText);
        await _page.ClickAsync("button[type=\"submit\"]");

        await Expect(_page.Locator($"text={cheepText}")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 10000
        });
    }
}