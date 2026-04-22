using Microsoft.Playwright;
using PlaywrightTests.Tests.Base;

namespace PlaywrightTests.Tests.ActualTests.AdminTests;

[Collection("Admin Auth Collection")]
public class ManageUsers_EditTests : AuthenticatedTestBase
{
    private const string UsersUrl = "http://localhost:5256/users";
    private const string CreateUserUrl = "http://localhost:5256/users/create";
    private const int DefaultUiTimeout = 10000;
    private const int LongUiTimeout = 20000;

    [Fact]
    public async Task Admin_Edit_User_Name_And_Save()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var originalName = $"PW EditTest {unique}";
        var updatedName = $"PW Updated {unique}";
        var email = $"edituser{unique}@test.com";

        try
        {
            await CreateUser(originalName, email);

            await OpenEditFormForUser(email);

            var inputs = Page.Locator("input.form-control");
            await Expect(inputs).ToHaveCountAsync(2, new() { Timeout = DefaultUiTimeout });
            await inputs.Nth(0).FillAsync(updatedName);
            await PauseForPhotoAsync("Edit User form with updated name before saving");

            var saveButton = Page.GetByRole(AriaRole.Button, new() { Name = "Save" });
            await Expect(saveButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
            await saveButton.ClickAsync();
            await WaitForUsersPageReady();

            var updatedUserRow = await WaitForUserRow(email, shouldExist: true, timeoutMs: LongUiTimeout);
            await Expect(updatedUserRow).ToContainTextAsync(updatedName, new() { Timeout = DefaultUiTimeout });
            await PauseForPhotoAsync("Users list showing saved updated user name");
        }
        finally
        {
            await DeleteUserIfExists(email);
        }
    }

    [Fact]
    public async Task Admin_Edit_User_Cancel()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var originalName = $"PW CancelTest {unique}";
        var unsavedName = $"PW ShouldNotSave {unique}";
        var email = $"canceluser{unique}@test.com";

        try
        {
            await CreateUser(originalName, email);

            await OpenEditFormForUser(email);

            var inputs = Page.Locator("input.form-control");
            await Expect(inputs).ToHaveCountAsync(2, new() { Timeout = DefaultUiTimeout });
            await inputs.Nth(0).FillAsync(unsavedName);
            await PauseForPhotoAsync("Edit User form with unsaved name before canceling");

            await Page.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

            await GoToUsersPage();

            var userRow = await WaitForUserRow(email, shouldExist: true, timeoutMs: LongUiTimeout);
            await Expect(userRow).ToContainTextAsync(originalName, new() { Timeout = DefaultUiTimeout });
            await Expect(userRow).Not.ToContainTextAsync(unsavedName, new() { Timeout = DefaultUiTimeout });
            await PauseForPhotoAsync("Users list after cancel keeps the original user name");
        }
        finally
        {
            await DeleteUserIfExists(email);
        }
    }

    [Fact]
    public async Task Admin_Create_User_Validation_Email_Required()
    {
        await GoToCreateUserPage();

        var inputs = Page.Locator("input.form-control");
        await Expect(inputs).ToHaveCountAsync(3, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(0).FillAsync("Test");
        await inputs.Nth(1).FillAsync("User");
        await inputs.Nth(2).FillAsync("");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Send Invite" }).ClickAsync();

        await Expect(Page.GetByText("Email is required."))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await PauseForPhotoAsync("Invite User form showing required email validation");

        Assert.Contains("/users/create", Page.Url);
    }

    [Fact]
    public async Task Admin_Create_User_Validation_Invalid_Email_Format()
    {
        await GoToCreateUserPage();

        var inputs = Page.Locator("input.form-control");
        await Expect(inputs).ToHaveCountAsync(3, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(0).FillAsync("Test");
        await inputs.Nth(1).FillAsync("User");
        await inputs.Nth(2).FillAsync("not-an-email");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Send Invite" }).ClickAsync();

        await Expect(Page.GetByText("Enter a valid email."))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await PauseForPhotoAsync("Invite User form showing invalid email validation");

        Assert.Contains("/users/create", Page.Url);
    }

    private async Task CreateUser(string name, string email)
    {
        await GoToUsersPage();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add User" }).ClickAsync();

        var inputs = Page.Locator("input.form-control");
        await Expect(inputs).ToHaveCountAsync(3, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(0).FillAsync(name);
        await inputs.Nth(1).FillAsync("Playwright");
        await inputs.Nth(2).FillAsync(email);

        var roleSelect = Page.Locator("select.form-select");
        await Expect(roleSelect).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await roleSelect.SelectOptionAsync(new SelectOptionValue { Label = "Manager" });
        await PauseForPhotoAsync("Invite User form filled before sending invite");

        var sendInviteButton = Page.GetByRole(AriaRole.Button, new() { Name = "Send Invite" });
        await Expect(sendInviteButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await sendInviteButton.ClickAsync();

        var errorAlert = Page.Locator(".alert-danger");
        var doneButton = Page.GetByRole(AriaRole.Button, new() { Name = "Done" });

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

            var userRow = Page.Locator(".accordion-item").Filter(new() { HasText = email }).First;
            if (await userRow.CountAsync() > 0 && await userRow.IsVisibleAsync())
                break;

            await Page.WaitForTimeoutAsync(1000);
        }

        await GoToUsersPage();
    }

    private async Task OpenEditFormForUser(string email)
    {
        await GoToUsersPage();

        var userRow = await WaitForUserRow(email, shouldExist: true, timeoutMs: LongUiTimeout);

        var accordionButton = userRow.Locator(".accordion-button").First;
        await Expect(accordionButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await accordionButton.ClickAsync();

        var editButton = userRow.Locator(".users-actions").GetByRole(AriaRole.Button, new() { Name = "Edit" }).First;
        await Expect(editButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await editButton.ScrollIntoViewIfNeededAsync();
        await editButton.EvaluateAsync("button => button.click()");

        await WaitForEditUserPageReady();
        await PauseForPhotoAsync("Edit User page opened from expanded user row");
    }

    private async Task DeleteUserIfExists(string email)
    {
        await GoToUsersPage();

        var userRow = Page.Locator(".accordion-item").Filter(new() { HasText = email }).First;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (await userRow.CountAsync() == 0)
                return;

            if (await userRow.IsVisibleAsync())
            {
                var accordionButton = userRow.Locator(".accordion-button").First;
                await accordionButton.ClickAsync();

                var deleteButton = userRow.GetByRole(AriaRole.Button, new() { Name = "Delete" }).First;
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

    private async Task GoToCreateUserPage()
    {
        await Page.GotoAsync(CreateUserUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForCreateUserPageReady();
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

    private async Task WaitForCreateUserPageReady()
    {
        await Page.WaitForURLAsync("**/users/create", new() { Timeout = LongUiTimeout });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Invite User" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        var inputs = Page.Locator("input.form-control");
        await Expect(inputs).ToHaveCountAsync(3, new() { Timeout = LongUiTimeout });
    }

    private async Task WaitForEditUserPageReady()
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(LongUiTimeout);
        while (DateTime.UtcNow < deadline)
        {
            if (Page.Url.Contains("/users/edit/"))
                break;

            await Page.WaitForTimeoutAsync(250);
        }

        if (!Page.Url.Contains("/users/edit/"))
            throw new Exception($"Expected edit user URL, but got: {Page.Url}");

        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("h3").Filter(new() { HasText = "Edit User" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        var inputs = Page.Locator("input.form-control");
        await Expect(inputs).ToHaveCountAsync(2, new() { Timeout = LongUiTimeout });
    }

    private async Task<ILocator> WaitForUserRow(string email, bool shouldExist, int timeoutMs = LongUiTimeout)
    {
        var userRow = Page.Locator(".accordion-item").Filter(new() { HasText = email }).First;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var count = await userRow.CountAsync();

            if (shouldExist)
            {
                if (count > 0 && await userRow.IsVisibleAsync())
                    return userRow;
            }
            else
            {
                if (count == 0)
                    return userRow;
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
