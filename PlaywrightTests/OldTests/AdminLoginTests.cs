using Microsoft.Playwright;

namespace PlaywrightTests;

public class AdminLoginTests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    public IPage Page { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        Page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task AdminCanLoginSuccessfully()
    {
        await Page.GotoAsync("http://3.20.198.36/signin");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("admin@test.com");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("testing1234");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        
        // Uncomment these lines to test navigation
        // await Page.GetByRole(AriaRole.Link, new() { Name = "Inventory" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = " Recipes" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = " Ingredients" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = " Manage Users" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
    }
}
