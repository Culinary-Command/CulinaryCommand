using Microsoft.Playwright;

namespace PlaywrightTests.Tests.Setup;

public class GlobalSetup : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await AuthHelper.SaveAuthState(browser, "authState.admin.json");

        await browser.CloseAsync();
        playwright.Dispose();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

