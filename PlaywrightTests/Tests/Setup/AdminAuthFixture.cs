
using Microsoft.Playwright;
using Xunit;

namespace PlaywrightTests.Tests.Setup;

public class AdminAuthFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        if (File.Exists("authState.admin.json"))
            return;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = false
        });

        await AuthHelper.SaveAuthState(browser, "authState.admin.json");
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
