using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace PlaywrightTests.Tests.Base;

public class AuthenticatedTestBase : PageTest
{
    protected virtual string AuthStatePath => "authState.admin.json";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            StorageStatePath = AuthStatePath,
            IgnoreHTTPSErrors = true
        };
    }

    protected async Task PauseForPhotoAsync(string label, IPage? page = null)
    {
        var enabled = Environment.GetEnvironmentVariable("PHOTO_PAUSE");
        if (!string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            return;

        Console.WriteLine($"PHOTO_PAUSE: {label}. Resume in the Playwright Inspector when ready.");
        await (page ?? Page).PauseAsync();
    }

    public override async Task DisposeAsync()
    {
        try
        {
            // Attempt logout at end of each test
            var profileButton = Page.GetByRole(AriaRole.Button, new() { Name = "Profile" });

            if (await profileButton.IsVisibleAsync(new() { Timeout = 2000 }))
            {
                await profileButton.ClickAsync();
            }

            var logout = Page.GetByText("Logout");

            if (await logout.IsVisibleAsync(new() { Timeout = 2000 }))
            {
                await logout.ClickAsync();
                await Page.WaitForURLAsync(url => url.Contains("/login"),
                    new() { Timeout = 5000 });
            }
        }
        catch
        {
            // Ignore failures (test might already be on login page, etc.)
        }

        await base.DisposeAsync();
    }
}
