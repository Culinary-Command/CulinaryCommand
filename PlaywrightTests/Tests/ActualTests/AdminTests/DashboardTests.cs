using PlaywrightTests.Tests.Base;
using PlaywrightTests.Tests.Setup;
using Microsoft.Playwright;

namespace PlaywrightTests.Tests.ActualTests.AdminTests;

[Collection("Admin Auth Collection")]
public class DashboardTests : AuthenticatedTestBase
{
    private const string BaseUrl = "http://localhost:5256";
    private const int DefaultUiTimeout = 15000;

    [Fact]
    public async Task Login_Route_ShouldRedirect_To_Cognito_Hosted_Login()
    {
        await Page.GotoAsync($"{BaseUrl}/login", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForUrlAsync(
            url => url.Contains("amazoncognito.com/login", StringComparison.OrdinalIgnoreCase),
            DefaultUiTimeout);

        Assert.Contains("amazoncognito.com/login", Page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("client_id=", Page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("redirect_uri=http://localhost:5256/signin-oidc", Page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("response_type=code", Page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_Dashboard_ShouldLoad()
    {
        await Page.GotoAsync($"{BaseUrl}/dashboard");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.DoesNotContain("/login", Page.Url);
        Assert.DoesNotContain("/account", Page.Url);
    }

    [Fact]
    public async Task Admin_Should_Land_On_Dashboard()
    {
        await Page.GotoAsync($"{BaseUrl}/dashboard");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Contains("/dashboard", Page.Url);
    }

    [Fact]
    public async Task Admin_Should_See_AddUser_Button()
    {
        await Page.GotoAsync($"{BaseUrl}/users");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var addUserButton = Page.GetByRole(AriaRole.Button, new() { Name = " Add User " });

        await Expect(addUserButton).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Admin_Should_Navigate_To_Users_Page()
    {
        await Page.GotoAsync($"{BaseUrl}/dashboard");

        await Page.ClickAsync("text=Users");

        await Page.WaitForURLAsync(url => url.Contains("/users"));

        Assert.Contains("/users", Page.Url);
    }

    [Fact]
    public async Task Admin_Should_Access_Admin_Page()
    {
        await Page.GotoAsync($"{BaseUrl}/users");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.DoesNotContain("/login", Page.Url);
    }

    private async Task WaitForUrlAsync(Func<string, bool> matches, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (matches(Page.Url))
                return;

            await Page.WaitForTimeoutAsync(250);
        }

        throw new TimeoutException($"Timed out waiting for expected URL. Current URL: {Page.Url}");
    }
}
