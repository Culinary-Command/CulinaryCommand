using Microsoft.Playwright;

namespace PlaywrightTests;

public class EmployeeLoginTests : IAsyncLifetime
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
    public async Task EmployeeCanLoginSuccessfully()
    {
        await Page.GotoAsync("http://3.20.198.36/signin");
        await Page.GetByText("Cook").ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("employee@test.com");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("testing123");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        
        // Uncomment these lines to test navigation
        // await Page.GetByRole(AriaRole.Link, new() { Name = "My Tasks" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = "Inventory" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Button, new() { Name = "Profile employee@test.com" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Button, new() { Name = "Profile employee@test.com" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
    }
}
