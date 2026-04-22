using Microsoft.Playwright;

namespace PlaywrightTests;

public class ManagerLoginTests : IAsyncLifetime
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
    public async Task ManagerCanLoginAndNavigate()
    {
        await Page.GotoAsync("http://3.20.198.36/signin");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("manager@test.com");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("testing123");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = "My Tasks" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Navigation).Locator("div").Filter(new() { HasText = "My Tasks" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { NameString = "Inventory", Exact = true }).ClickAsync();
        // await Page.Locator("div").Filter(new() { HasText = "Inventory" }).Nth(3).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = " Recipes" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = " Ingredients" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = " Inventory" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        // await Page.GetByRole(AriaRole.Button, new() { Name = "Profile manager@test.com" }).ClickAsync();
    }
}
