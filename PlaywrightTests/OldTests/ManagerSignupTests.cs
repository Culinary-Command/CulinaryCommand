using Microsoft.Playwright;

namespace PlaywrightTests;

public class ManagerSignupTests : IAsyncLifetime
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
    public async Task ManagerCanSignUpSuccessfully()
    {
        await Page.GotoAsync("http://3.20.198.36/signin");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Sign up" }).ClickAsync();
        await Page.GetByText("Manager").ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "First name" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "First name" }).FillAsync("Manager");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "First name" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Last name" }).FillAsync("Test");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Last name" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Enter your email" }).FillAsync("manager@test.com");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Create a password" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Create a password" }).FillAsync("testing123");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm your password" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm your password" }).FillAsync("testing123");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign Up" }).ClickAsync();
    }
}
