using Microsoft.Playwright;

namespace PlaywrightTests;

public class AdminSignupTests : IAsyncLifetime
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
    public async Task AdminCanCreateRestaurantAccount()
    {
        await Page.GotoAsync("http://3.20.198.36/signin");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Create Restaurant" }).ClickAsync();
        await Page.Locator("input[name=\"Signup.Company.Name\"]").ClickAsync();
        await Page.Locator("input[name=\"Signup.Company.Name\"]").FillAsync("Cyclone Cafe");
        await Page.Locator("input[name=\"Signup.Company.Address\"]").ClickAsync();
        await Page.Locator("input[name=\"Signup.Company.Address\"]").FillAsync("1234 Main St");
        await Page.Locator("input[name=\"Signup.Company.City\"]").ClickAsync();
        await Page.Locator("input[name=\"Signup.Company.City\"]").FillAsync("Ames");
        await Page.Locator("input[name=\"Signup.Company.State\"]").ClickAsync();
        await Page.Locator("input[name=\"Signup.Company.State\"]").FillAsync("IA");
        await Page.Locator("input[name=\"Signup.Company.ZipCode\"]").ClickAsync();
        await Page.Locator("input[name=\"Signup.Company.ZipCode\"]").FillAsync("12345");
        await Page.Locator("input[name=\"FirstName\"]").ClickAsync();
        await Page.Locator("input[name=\"FirstName\"]").FillAsync("Admin");
        await Page.Locator("input[name=\"LastName\"]").ClickAsync();
        await Page.Locator("input[name=\"LastName\"]").FillAsync("Test");
        await Page.Locator("input[name=\"Signup.Admin.Email\"]").ClickAsync();
        await Page.Locator("input[name=\"Signup.Admin.Email\"]").FillAsync("admin@test.com");
        await Page.Locator("input[name=\"Signup.Admin.Password\"]").ClickAsync();
        await Page.Locator("input[name=\"Signup.Admin.Password\"]").FillAsync("testing1234");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Account" }).ClickAsync();
    }
}
