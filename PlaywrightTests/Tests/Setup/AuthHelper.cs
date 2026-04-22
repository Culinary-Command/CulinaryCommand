
using Microsoft.Playwright;

namespace PlaywrightTests.Tests.Setup;

public static class AuthHelper
{
    public static async Task SaveAuthState(IBrowser browser, string statePath = "authState.admin.json")
    {
        var username = Environment.GetEnvironmentVariable("PLAYWRIGHT_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("PLAYWRIGHT_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Missing PLAYWRIGHT_ADMIN_EMAIL or PLAYWRIGHT_ADMIN_PASSWORD.");

        var context = await browser.NewContextAsync(new()
        {
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync("http://localhost:5256/login");

        // If your local login page has a sign-in button, click it.
        var signInButton = page.GetByRole(AriaRole.Button, new() { Name = "Sign In" });
        if (await signInButton.IsVisibleAsync(new() { Timeout = 3000 }))
        {
            await signInButton.ClickAsync();
        }

        // Wait for either Cognito OR successful redirect back into app
        try
        {
            await page.Locator("input[name='username']").WaitForAsync(new() { Timeout = 5000 });

            await page.FillAsync("input[name='username']", username);
            var nextButton = page.GetByRole(AriaRole.Button, new() { Name = "Next" });

            if (await nextButton.IsVisibleAsync(new() { Timeout = 5000 }))
            {
                await nextButton.ClickAsync();
            }

            await page.FillAsync("input[name='password']", password);
            var continueButton = page.GetByRole(AriaRole.Button, new() { Name = "Continue" });

            if (await continueButton.IsVisibleAsync(new() { Timeout = 5000 }))
            {
                await continueButton.ClickAsync();
            }
            //await page.ClickAsync("input[type='submit']");
        }
        catch (TimeoutException)
        {
            // Fine: maybe already logged in and redirected past Cognito
        }

        await page.WaitForURLAsync(url =>
            url.Contains("/dashboard") ||
            url.Contains("/tasks") ||
            url.Contains("/onboarding"),
            new() { Timeout = 30000 });

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);



        await context.StorageStateAsync(new() { Path = statePath });
        await context.CloseAsync();
    }
}
