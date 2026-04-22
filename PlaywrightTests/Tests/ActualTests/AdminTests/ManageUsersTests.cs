using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using PlaywrightTests.Tests.Base;
using PlaywrightTests.Tests.Setup;

namespace PlaywrightTests.Tests.ActualTests.AdminTests;

[Collection("Admin Auth Collection")]
public class ManageUsersTests : AuthenticatedTestBase
{
    private const string UsersUrl = "http://localhost:5256/users";
    private const int DefaultUiTimeout = 10000;
    private const int LongUiTimeout = 20000;

    [Fact]
    public async Task Admin_User_Lifecycle_Create_VerifyInvite_Delete()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var firstName = "PW";
        var lastName = $"Manager{unique}";
        var email = $"pwmanager{unique}@test.com";

        try
        {
            await CreateUser(firstName, lastName, email);
            await VerifyUserExists(email);
            //await PauseForPhotoAsync("new invited user visible in the Users list");

            var inviteUrl = await GetInviteLink(email);
            await OpenInviteAndVerifySetupPage(inviteUrl);

            await DeleteUser(email);
            await VerifyUserDeleted(email);
        }
        finally
        {
            await DeleteUserIfExists(email);
        }
    }

    private async Task CreateUser(string firstName, string lastName, string email)
    {
        await GoToUsersPage();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add User" })
            .ClickAsync();

        var inputs = Page.Locator("input.form-control");
        await Expect(inputs).ToHaveCountAsync(3, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(0).FillAsync(firstName);
        await inputs.Nth(1).FillAsync(lastName);
        await inputs.Nth(2).FillAsync(email);

        var roleSelect = Page.Locator("select.form-select");
        await Expect(roleSelect).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await roleSelect.SelectOptionAsync(new SelectOptionValue { Label = "Manager" });
        await PauseForPhotoAsync("Add User form filled before sending invite");

        var sendInviteButton = Page.GetByRole(AriaRole.Button, new() { Name = "Send Invite" });
        await Expect(sendInviteButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await sendInviteButton.ClickAsync();

        var errorAlert = Page.Locator(".alert-danger");
        var doneButton = Page.GetByRole(AriaRole.Button, new() { Name = "Done" });

        // Wait for one of the expected outcomes after clicking Send Invite:
        // 1) an error alert
        // 2) a done/success button
        // 3) the created user appears on the users page after refresh
        for (var attempt = 0; attempt < 8; attempt++)
        {
            if (await errorAlert.IsVisibleAsync())
            {
                var errorText = await errorAlert.InnerTextAsync();
                throw new Exception($"Invite flow failed: {errorText}");
            }

            if (await doneButton.IsVisibleAsync())
            {
                await doneButton.ClickAsync();
                break;
            }

            var userItem = Page.Locator(".accordion-item").Filter(new() { HasText = email }).First;
            if (await userItem.CountAsync() > 0 && await userItem.IsVisibleAsync())
            {
                break;
            }

            await Page.WaitForTimeoutAsync(1000);
        }

        await GoToUsersPage();
    }

    private async Task VerifyUserExists(string email)
    {
        var userItem = await WaitForUserRow(email, shouldExist: true, timeoutMs: LongUiTimeout);

        await Expect(userItem).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task<string> GetInviteLink(string email)
    {
        await GoToUsersPage();

        var userItem = await WaitForUserRow(email, shouldExist: true, timeoutMs: LongUiTimeout);

        var accordionButton = userItem.Locator(".accordion-button").First;
        await Expect(accordionButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await accordionButton.ClickAsync();

        var invitePill = userItem.Locator(".invite-pill").First;
        await Expect(invitePill).ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await PauseForPhotoAsync("expanded invited user row showing setup link");

        var inviteUrl = (await invitePill.InnerTextAsync())?.Trim();

        Assert.False(string.IsNullOrWhiteSpace(inviteUrl));
        Assert.Contains("/account/setup?token=", inviteUrl);

        return inviteUrl!;
    }

    private async Task OpenInviteAndVerifySetupPage(string inviteUrl)
    {
        var browser = Page.Context.Browser!;
        await using var context = await browser.NewContextAsync();
        var invitePage = await context.NewPageAsync();

        await invitePage.GotoAsync(inviteUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await invitePage.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(
            invitePage.GetByRole(AriaRole.Heading, new() { Name = "Activate Your Account" })
        ).ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        var passwordInputs = invitePage.Locator("input.form-control");
        await Expect(passwordInputs).ToHaveCountAsync(2, new() { Timeout = DefaultUiTimeout });

        await Expect(passwordInputs.Nth(0)).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await Expect(passwordInputs.Nth(1)).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await PauseForPhotoAsync("invited user's Activate Your Account page", invitePage);

        await invitePage.CloseAsync();
    }

    private async Task DeleteUser(string email)
    {
        await GoToUsersPage();

        var userItem = await WaitForUserRow(email, shouldExist: true, timeoutMs: LongUiTimeout);

        var accordionButton = userItem.Locator(".accordion-button").First;
        await Expect(accordionButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await accordionButton.ClickAsync();

        var deleteButton = userItem.GetByRole(AriaRole.Button, new() { Name = "Delete" }).First;
        await Expect(deleteButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await Expect(deleteButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await deleteButton.ClickAsync();

        await Page.WaitForTimeoutAsync(1000);
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForUsersPageReady();
    }

    private async Task VerifyUserDeleted(string email)
    {
        await WaitForUserRow(email, shouldExist: false, timeoutMs: LongUiTimeout);
    }

    private async Task DeleteUserIfExists(string email)
    {
        await GoToUsersPage();

        var userItem = Page.Locator(".accordion-item").Filter(new() { HasText = email }).First;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (await userItem.CountAsync() == 0)
                return;

            if (await userItem.IsVisibleAsync())
            {
                var accordionButton = userItem.Locator(".accordion-button").First;
                await accordionButton.ClickAsync();

                var deleteButton = userItem.GetByRole(AriaRole.Button, new() { Name = "Delete" }).First;
                if (await deleteButton.IsVisibleAsync())
                {
                    await deleteButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(1000);
                    await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    await WaitForUsersPageReady();
                }
            }

            await Page.WaitForTimeoutAsync(1000);
        }
    }

    private async Task GoToUsersPage()
    {
        await Page.GotoAsync(UsersUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForUsersPageReady();
    }

    private async Task WaitForUsersPageReady()
    {
        await Page.WaitForURLAsync("**/users", new() { Timeout = LongUiTimeout });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var addUserButton = Page.GetByRole(AriaRole.Button, new() { Name = "Add User" });
        await Expect(addUserButton).ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        await Page.WaitForTimeoutAsync(500);
    }

    private async Task<ILocator> WaitForUserRow(string email, bool shouldExist, int timeoutMs = LongUiTimeout)
    {
        var userItem = Page.Locator(".accordion-item").Filter(new() { HasText = email }).First;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var count = await userItem.CountAsync();

            if (shouldExist)
            {
                if (count > 0 && await userItem.IsVisibleAsync())
                    return userItem;
            }
            else
            {
                if (count == 0)
                    return userItem;
            }

            await Page.WaitForTimeoutAsync(1000);
            await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForUsersPageReady();
        }

        if (shouldExist)
            throw new Exception($"User row for '{email}' did not appear within {timeoutMs}ms.");

        throw new Exception($"User row for '{email}' was still present after {timeoutMs}ms.");
    }
}
