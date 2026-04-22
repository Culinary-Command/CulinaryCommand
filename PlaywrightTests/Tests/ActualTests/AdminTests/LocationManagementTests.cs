using Microsoft.Playwright;
using PlaywrightTests.Tests.Base;

namespace PlaywrightTests.Tests.ActualTests.AdminTests;

[Collection("Admin Auth Collection")]
public class LocationManagementTests : AuthenticatedTestBase
{
    private const string DashboardUrl = "http://localhost:5256/dashboard";
    private const string SettingsLocationsUrl = "http://localhost:5256/settings/locations";
    private const int DefaultUiTimeout = 10000;
    private const int LongUiTimeout = 20000;

    [Fact]
    public async Task Admin_LocationManagement_Lifecycle_Create_Edit_Configure_Delete()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var locationName = $"PW Location {unique}";
        var updatedLocationName = $"PW Location Edited {unique}";

        try
        {
            await NavigateToLocationsThroughSettingsDropdown();

            await CreateLocation(
                locationName,
                address: "123 Playwright Ave",
                city: "Ames",
                state: "IA",
                zipCode: "50010");

            await VerifyLocationExists(locationName);
            await PauseForPhotoAsync("new location visible in Settings Locations");

            await OpenLocationUsersPanel(locationName);
            await PauseForPhotoAsync("location Manage Users panel open");

            await EditLocation(
                locationName,
                updatedLocationName,
                address: "456 Browser Blvd",
                city: "Des Moines",
                state: "IA",
                zipCode: "50309");

            await VerifyLocationExists(updatedLocationName);
            await PauseForPhotoAsync("edited location visible in Settings Locations");

            await OpenConfigureRestaurant(updatedLocationName);
            await PauseForPhotoAsync("Configure Location page for Playwright location");

            await DeleteLocation(updatedLocationName);
            await VerifyLocationDeleted(updatedLocationName);
        }
        finally
        {
            await CleanupLocationIfExists(updatedLocationName);
            await CleanupLocationIfExists(locationName);
        }
    }

    private async Task NavigateToLocationsThroughSettingsDropdown()
    {
        await Page.GotoAsync(DashboardUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.DoesNotContain("/login", Page.Url, StringComparison.OrdinalIgnoreCase);

        if (await Page.GetByText("not signed in").CountAsync() > 0)
            throw new InvalidOperationException("Dashboard rendered the signed-out state. Refresh authState.admin.json or set PLAYWRIGHT_ADMIN_EMAIL and PLAYWRIGHT_ADMIN_PASSWORD before running this test.");

        var profileButton = Page.Locator("#profileMenu");
        await Expect(profileButton).ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await profileButton.ClickAsync();

        var settingsLink = Page.GetByRole(AriaRole.Link, new() { Name = "Settings" });
        await Expect(settingsLink).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await settingsLink.ClickAsync();

        await WaitForSettingsPageReady();

        var locationsTab = Page.GetByRole(AriaRole.Button, new() { Name = "Locations" });
        await Expect(locationsTab).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await locationsTab.ClickAsync();

        await WaitForLocationsPageReady();
        await PauseForPhotoAsync("Settings Locations page opened from the profile dropdown");
    }

    private async Task CreateLocation(
        string name,
        string address,
        string city,
        string state,
        string zipCode)
    {
        await GoToLocationsPage();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Location" }).ClickAsync();

        var modal = GetLocationModal("Add Location");
        await Expect(modal).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        await FillLocationModal(modal, name, address, city, state, zipCode);
        await PauseForPhotoAsync("Add Location modal filled before saving");

        await modal.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(modal).ToBeHiddenAsync(new() { Timeout = LongUiTimeout });

        await WaitForLocationCard(name, shouldExist: true, timeoutMs: LongUiTimeout);
    }

    private async Task EditLocation(
        string currentName,
        string updatedName,
        string address,
        string city,
        string state,
        string zipCode)
    {
        await GoToLocationsPage();

        var locationCard = await WaitForLocationCard(currentName, shouldExist: true, timeoutMs: LongUiTimeout);
        await locationCard.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();

        var modal = GetLocationModal("Edit Location");
        await Expect(modal).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        await FillLocationModal(modal, updatedName, address, city, state, zipCode);
        await PauseForPhotoAsync("Edit Location modal filled before saving");

        await modal.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(modal).ToBeHiddenAsync(new() { Timeout = LongUiTimeout });

        await WaitForLocationCard(updatedName, shouldExist: true, timeoutMs: LongUiTimeout);
    }

    private async Task OpenLocationUsersPanel(string locationName)
    {
        await GoToLocationsPage();

        var locationCard = await WaitForLocationCard(locationName, shouldExist: true, timeoutMs: LongUiTimeout);
        var manageUsersButton = locationCard.GetByRole(AriaRole.Button, new() { Name = "Manage Users" });
        await Expect(manageUsersButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await manageUsersButton.ClickAsync();

        await Expect(locationCard.GetByRole(AriaRole.Heading, new() { Name = "Users Assigned to This Location" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await Expect(locationCard.GetByRole(AriaRole.Button, new() { Name = "Invite User" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await Expect(locationCard.GetByRole(AriaRole.Button, new() { Name = "Add Existing User" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task OpenConfigureRestaurant(string locationName)
    {
        await GoToLocationsPage();

        var locationCard = await WaitForLocationCard(locationName, shouldExist: true, timeoutMs: LongUiTimeout);
        var configureButton = locationCard.GetByRole(AriaRole.Button, new() { Name = "Configure Restaurant" });
        await Expect(configureButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await configureButton.ClickAsync();

        await Page.WaitForURLAsync("**/settings/locations/configure/*", new() { Timeout = LongUiTimeout });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Configure Location" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Vendors" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Measurement Units" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Storage Locations" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task DeleteLocation(string locationName)
    {
        await GoToLocationsPage();

        var locationCard = await WaitForLocationCard(locationName, shouldExist: true, timeoutMs: LongUiTimeout);
        var deleteButton = locationCard.GetByRole(AriaRole.Button, new() { Name = "Delete" });
        await Expect(deleteButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await deleteButton.ClickAsync();

        await WaitForLocationCard(locationName, shouldExist: false, timeoutMs: LongUiTimeout);
    }

    private async Task DeleteLocationIfExists(string locationName)
    {
        await GoToLocationsPage();

        var locationCard = GetLocationCard(locationName);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (await locationCard.CountAsync() == 0)
                return;

            if (await locationCard.IsVisibleAsync())
            {
                var deleteButton = locationCard.GetByRole(AriaRole.Button, new() { Name = "Delete" });
                if (await deleteButton.IsVisibleAsync())
                {
                    await deleteButton.ClickAsync();
                    await WaitForLocationCard(locationName, shouldExist: false, timeoutMs: LongUiTimeout);
                    return;
                }
            }

            await Page.WaitForTimeoutAsync(1000);
            await GoToLocationsPage();
            locationCard = GetLocationCard(locationName);
        }
    }

    private async Task VerifyLocationExists(string locationName)
    {
        var locationCard = await WaitForLocationCard(locationName, shouldExist: true, timeoutMs: LongUiTimeout);

        await Expect(locationCard).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task VerifyLocationDeleted(string locationName)
    {
        await WaitForLocationCard(locationName, shouldExist: false, timeoutMs: LongUiTimeout);
    }

    private async Task GoToLocationsPage()
    {
        await Page.GotoAsync(SettingsLocationsUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForLocationsPageReady();
    }

    private async Task WaitForSettingsPageReady()
    {
        await WaitForUrlAsync(
            url => url.Contains("/settings", StringComparison.OrdinalIgnoreCase),
            LongUiTimeout,
            "settings page");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Settings", Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
    }

    private async Task WaitForLocationsPageReady()
    {
        await WaitForUrlAsync(
            url => url.Contains("/settings/locations", StringComparison.OrdinalIgnoreCase) &&
                   !url.Contains("/configure/", StringComparison.OrdinalIgnoreCase),
            LongUiTimeout,
            "settings locations page");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Company Locations" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add Location" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
    }

    private ILocator GetLocationModal(string title)
    {
        return Page.Locator(".modal-content").Filter(new() { HasText = title }).Last;
    }

    private async Task CleanupLocationIfExists(string locationName)
    {
        try
        {
            await DeleteLocationIfExists(locationName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cleanup failed for location '{locationName}': {ex.Message}");
        }
    }

    private async Task WaitForUrlAsync(Func<string, bool> matches, int timeoutMs, string description)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            if (matches(Page.Url))
                return;

            await Page.WaitForTimeoutAsync(250);
        }

        throw new TimeoutException($"Timed out waiting for {description}. Current URL: {Page.Url}");
    }

    private async Task FillLocationModal(
        ILocator modal,
        string name,
        string address,
        string city,
        string state,
        string zipCode)
    {
        var inputs = modal.Locator("input.form-control");
        await Expect(inputs).ToHaveCountAsync(5, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(0).FillAsync(name);
        await inputs.Nth(1).FillAsync(address);
        await inputs.Nth(2).FillAsync(city);
        await inputs.Nth(3).FillAsync(state);
        await inputs.Nth(4).FillAsync(zipCode);
    }

    private ILocator GetLocationCard(string locationName)
    {
        return Page.Locator(".location-admin-view .card").Filter(new() { HasText = locationName }).First;
    }

    private async Task<ILocator> WaitForLocationCard(string locationName, bool shouldExist, int timeoutMs)
    {
        var locationCard = GetLocationCard(locationName);
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var count = await locationCard.CountAsync();

            if (shouldExist)
            {
                if (count > 0 && await locationCard.IsVisibleAsync())
                    return locationCard;
            }
            else if (count == 0)
            {
                return locationCard;
            }

            await Page.WaitForTimeoutAsync(1000);
        }

        if (shouldExist)
            throw new Exception($"Location card for '{locationName}' did not appear within {timeoutMs}ms.");

        throw new Exception($"Location card for '{locationName}' was still present after {timeoutMs}ms.");
    }
}
