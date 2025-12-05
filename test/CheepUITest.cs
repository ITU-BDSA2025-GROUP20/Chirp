/*using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests;

// This is the ONLY way that works with top-level Program.cs in global namespace
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Force the environment so the app starts correctly
        builder.UseEnvironment("Development");

        // This is the magic line — forces the top-level Program to be executed
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartup>(new Startup());
        });

        return base.CreateHost(builder);
    }
}

// Dummy startup class just to satisfy the host builder
public class Startup
{
    public void ConfigureServices(IServiceCollection services) { }
    public void Configure(IApplicationBuilder app) { }
}

public class CheepUITests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public CheepUITests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task CheepBox_VisibleOnlyWhenLoggedIn()
    {
        var url = _factory.Server.BaseAddress.ToString();

        await _page!.GotoAsync(url);
        await Assertions.Expect(_page.Locator(".cheepbox")).ToBeHiddenAsync();

        await _page.GetByRole(AriaRole.Link, new() { Name = "register" }).ClickAsync();
        await _page.FillAsync("input[name=\"Input.Email\"]", $"test{Guid.NewGuid()}@example.com");
        await _page.FillAsync("input[name=\"Input.Password\"]", "Password123!");
        await _page.FillAsync("input[name=\"Input.ConfirmPassword\"]", "Password123!");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();

        await Assertions.Expect(_page.Locator(".cheepbox")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CannotPostCheepOver160Characters_ShowsError()
    {
        var url = _factory.Server.BaseAddress.ToString();

        await _page!.GotoAsync($"{url}Identity/Account/Register");
        await _page.FillAsync("input[name=\"Input.Email\"]", $"long{Guid.NewGuid()}@example.com");
        await _page.FillAsync("input[name=\"Input.Password\"]", "Password123!");
        await _page.FillAsync("input[name=\"Input.ConfirmPassword\"]", "Password123!");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();

        await _page.FillAsync("textarea[asp-for=\"Text\"]", new string('A', 161));
        await _page.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();

        await Assertions.Expect(_page.GetByText("Cheeps cannot be longer than 160 characters"))
                       .ToBeVisibleAsync();
    }
}*/