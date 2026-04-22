using Microsoft.Playwright;
using PlaywrightTests.Tests.Base;

namespace PlaywrightTests.Tests.ActualTests.AdminTests;

[Collection("Admin Auth Collection")]
public class InventoryManagementTests : AuthenticatedTestBase
{
    private const string DashboardUrl = "http://localhost:5256/dashboard";
    private const string InventoryCatalogUrl = "http://localhost:5256/inventory-catalog";
    private const string InventorySearchPlaceholder = "Search Ingredients by name, category, or supplier...";
    private const int DefaultUiTimeout = 10000;
    private const int LongUiTimeout = 20000;

    private string? _inventoryManagementUrl;

    [Fact]
    public async Task Admin_InventoryManagement_Lifecycle_Create_Edit_Filter_Delete()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var itemName = $"PW Inventory Item {unique}";
        var itemSku = $"PW-INV-{unique}";
        var updatedNotes = $"Updated by Playwright {unique}";

        try
        {
            await CreateInventoryCatalogItem(itemName, itemSku);
            await VerifyInventoryItemExists(itemName);

            await EditInventoryItemToLowStock(itemName, currentQuantity: "2", reorderLevel: "5", updatedNotes);
            await VerifyInventoryItemExists(itemName);

            await FilterToLowStockAndVerifyItem(itemName);

            await DeleteInventoryItem(itemName);
            await VerifyInventoryItemDeleted(itemName);
        }
        finally
        {
            await DeleteInventoryItemIfExists(itemName);
        }
    }

    [Fact]
    public async Task Admin_InventoryManagement_Search_ShouldFilter_BySku_And_Name()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var firstItemName = $"PW Search Item {unique}";
        var firstItemSku = $"PW-SRCH-{unique}";
        var secondItemName = $"PW Search Other {unique}";
        var secondItemSku = $"PW-OTHER-{unique}";

        try
        {
            await CreateInventoryCatalogItem(firstItemName, firstItemSku);
            await CreateInventoryCatalogItem(secondItemName, secondItemSku);

            await GoToInventoryManagementPage();

            await FilterInventoryBySearchTerm(firstItemSku);
            await VerifyInventoryItemVisibleInCurrentView(firstItemName, shouldBeVisible: true);
            await VerifyInventoryItemVisibleInCurrentView(secondItemName, shouldBeVisible: false);

            await FilterInventoryBySearchTerm(secondItemName);
            await VerifyInventoryItemVisibleInCurrentView(secondItemName, shouldBeVisible: true);
            await VerifyInventoryItemVisibleInCurrentView(firstItemName, shouldBeVisible: false);
        }
        finally
        {
            await DeleteInventoryItemIfExists(firstItemName);
            await DeleteInventoryItemIfExists(secondItemName);
        }
    }

    [Fact]
    public async Task Admin_InventoryManagement_Delete_Cancel_ShouldKeep_Item()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var itemName = $"PW Delete Cancel Item {unique}";
        var itemSku = $"PW-KEEP-{unique}";

        try
        {
            await CreateInventoryCatalogItem(itemName, itemSku);
            await VerifyInventoryItemExists(itemName);

            await GoToInventoryManagementPage();
            await FilterInventoryBySearchTerm(itemName);

            var itemRow = await WaitForInventoryItemRow(itemName, shouldExist: true, timeoutMs: LongUiTimeout);
            var deleteButton = itemRow.GetByTitle("Delete").First;
            await Expect(deleteButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

            await HandleDeleteDialogAsync(deleteButton, accept: false);

            await VerifyInventoryItemVisibleInCurrentView(itemName, shouldBeVisible: true);
            await VerifyInventoryItemExists(itemName);
        }
        finally
        {
            await DeleteInventoryItemIfExists(itemName);
        }
    }

    private async Task CreateInventoryCatalogItem(
        string itemName,
        string itemSku,
        string currentQuantity = "12",
        string reorderLevel = "4")
    {
        await Page.GotoAsync(InventoryCatalogUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForInventoryCatalogPageReady();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Item" }).ClickAsync();

        var modal = Page.Locator(".modal-dialog-custom").Last;
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Add New Item" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        var inputs = modal.Locator("input.form-control");
        var selects = modal.Locator("select.form-control");

        await Expect(inputs).ToHaveCountAsync(5, new() { Timeout = DefaultUiTimeout });
        await Expect(selects).ToHaveCountAsync(4, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(0).FillAsync(itemName);
        await inputs.Nth(1).FillAsync(itemSku);
        await selects.Nth(1).SelectOptionAsync(new SelectOptionValue { Label = "Produce" });
        await selects.Nth(3).SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await inputs.Nth(2).FillAsync("2.50");
        await inputs.Nth(3).FillAsync(currentQuantity);
        await inputs.Nth(4).FillAsync(reorderLevel);

        var addButton = modal.GetByRole(AriaRole.Button, new() { Name = "Add Item" });
        await Expect(addButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await addButton.EvaluateAsync("button => button.click()");

        await Page.WaitForTimeoutAsync(1000);
    }

    private async Task EditInventoryItemToLowStock(string itemName, string currentQuantity, string reorderLevel, string notes)
    {
        await GoToInventoryManagementPage();

        await FilterInventoryBySearchTerm(itemName);

        var itemRow = await WaitForInventoryItemRow(itemName, shouldExist: true, timeoutMs: LongUiTimeout);

        var editButton = itemRow.GetByTitle("Edit").First;
        await Expect(editButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await editButton.ClickAsync();

        var modal = await WaitForInventoryModal("Edit Item");
        var inputs = modal.Locator("input.form-control");

        await Expect(inputs).ToHaveCountAsync(8, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(2).FillAsync(currentQuantity);
        await inputs.Nth(4).FillAsync(reorderLevel);
        await inputs.Nth(7).FillAsync(notes);
        await inputs.Nth(7).PressAsync("Tab");
        await Page.WaitForTimeoutAsync(300);

        var saveButton = modal.GetByRole(AriaRole.Button, new() { Name = "Save Changes" });
        await Expect(saveButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await saveButton.EvaluateAsync("button => button.click()");

        await Page.WaitForTimeoutAsync(1000);
    }

    private async Task FilterToLowStockAndVerifyItem(string itemName)
    {
        await GoToInventoryManagementPage();

        await SelectInventoryTab("Low Stock");

        await FilterInventoryBySearchTerm(itemName);

        var lowStockRow = await WaitForInventoryItemRow(itemName, shouldExist: true, timeoutMs: LongUiTimeout);
        await Expect(lowStockRow).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task DeleteInventoryItem(string itemName)
    {
        await GoToInventoryManagementPage();

        await FilterInventoryBySearchTerm(itemName);

        var itemRow = await WaitForInventoryItemRow(itemName, shouldExist: true, timeoutMs: LongUiTimeout);

        var deleteButton = itemRow.GetByTitle("Delete").First;
        await Expect(deleteButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        await HandleDeleteDialogAsync(deleteButton, accept: true);

        await Page.WaitForTimeoutAsync(1000);
    }

    private async Task VerifyInventoryItemExists(string itemName)
    {
        await GoToInventoryManagementPage();

        await FilterInventoryBySearchTerm(itemName);

        var itemRow = await WaitForInventoryItemRow(itemName, shouldExist: true, timeoutMs: LongUiTimeout);
        await Expect(itemRow).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task VerifyInventoryItemDeleted(string itemName)
    {
        await GoToInventoryManagementPage();

        await FilterInventoryBySearchTerm(itemName);

        await WaitForInventoryItemRow(itemName, shouldExist: false, timeoutMs: LongUiTimeout);
    }

    private async Task DeleteInventoryItemIfExists(string itemName)
    {
        await GoToInventoryManagementPage();

        await FilterInventoryBySearchTerm(itemName);

        var itemRow = Page.Locator("table.inventory-table tbody tr").Filter(new() { HasText = itemName }).First;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (await itemRow.CountAsync() == 0)
                return;

            if (await itemRow.IsVisibleAsync())
            {
                var deleteButton = itemRow.GetByTitle("Delete").First;
                if (await deleteButton.IsVisibleAsync())
                {
                    await HandleDeleteDialogAsync(deleteButton, accept: true);

                    await Page.WaitForTimeoutAsync(1000);
                }
            }

            await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForInventoryManagementPageReady();
            await FilterInventoryBySearchTerm(itemName);
        }
    }

    private async Task GoToInventoryManagementPage()
    {
        if (string.IsNullOrWhiteSpace(_inventoryManagementUrl))
        {
            await Page.GotoAsync(DashboardUrl, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var inventoryNavLink = Page.Locator("a.nav-link").Filter(new() { HasText = "Inventory" }).Last;
            await Expect(inventoryNavLink).ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
            await inventoryNavLink.ClickAsync();

            var deadline = DateTime.UtcNow.AddMilliseconds(LongUiTimeout);
            while (DateTime.UtcNow < deadline)
            {
                if (Page.Url.Contains("/inventory-management/"))
                    break;

                await Page.WaitForTimeoutAsync(250);
            }

            if (!Page.Url.Contains("/inventory-management/"))
                throw new Exception($"Expected inventory management URL, but got: {Page.Url}");

            _inventoryManagementUrl = Page.Url;
        }
        else
        {
            await Page.GotoAsync(_inventoryManagementUrl, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
        }

        await WaitForInventoryManagementPageReady();
    }

    private async Task WaitForInventoryManagementPageReady()
    {
        await Page.WaitForURLAsync(url => url.Contains("/inventory-management/"), new() { Timeout = LongUiTimeout });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Inventory Management" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add Item" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        await Page.WaitForTimeoutAsync(500);
    }

    private async Task WaitForInventoryCatalogPageReady()
    {
        await Page.WaitForURLAsync("**/inventory-catalog", new() { Timeout = LongUiTimeout });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Inventory Catalog" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        await Page.WaitForTimeoutAsync(500);
    }

    private async Task<ILocator> WaitForInventoryModal(string headingText)
    {
        var modal = Page.Locator(".modal-dialog-custom").Last;
        await Expect(modal.Locator("h3").Filter(new() { HasText = headingText }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        return modal;
    }

    private async Task FilterInventoryBySearchTerm(string searchTerm)
    {
        var searchInput = Page.GetByPlaceholder(InventorySearchPlaceholder);
        await Expect(searchInput).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await searchInput.FillAsync(searchTerm);
        await Page.WaitForTimeoutAsync(500);
    }

    private async Task SelectInventoryTab(string tabText)
    {
        var tab = Page.Locator(".tab-row .tab-btn").Filter(new() { HasText = tabText }).First;
        await Expect(tab).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await tab.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
    }

    private async Task VerifyInventoryItemVisibleInCurrentView(string itemName, bool shouldBeVisible)
    {
        var row = Page.Locator("table.inventory-table tbody tr").Filter(new() { HasText = itemName }).First;
        var deadline = DateTime.UtcNow.AddMilliseconds(DefaultUiTimeout);

        while (DateTime.UtcNow < deadline)
        {
            var count = await row.CountAsync();

            if (shouldBeVisible)
            {
                if (count > 0 && await row.IsVisibleAsync())
                    return;
            }
            else
            {
                if (count == 0 || !await row.IsVisibleAsync())
                    return;
            }

            await Page.WaitForTimeoutAsync(250);
        }

        if (shouldBeVisible)
            throw new Exception($"Expected inventory item '{itemName}' to be visible in the current inventory view.");

        throw new Exception($"Expected inventory item '{itemName}' to be hidden in the current inventory view.");
    }

    private async Task HandleDeleteDialogAsync(ILocator deleteButton, bool accept)
    {
        var dialogTcs = new TaskCompletionSource<IDialog>();
        void Handler(object? _, IDialog dialog) => dialogTcs.TrySetResult(dialog);

        Page.Dialog += Handler;
        try
        {
            await deleteButton.ClickAsync();

            var dialog = await dialogTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (accept)
                await dialog.AcceptAsync();
            else
                await dialog.DismissAsync();
        }
        finally
        {
            Page.Dialog -= Handler;
        }
    }

    private async Task<ILocator> WaitForInventoryItemRow(string itemName, bool shouldExist, int timeoutMs = LongUiTimeout)
    {
        var itemRow = Page.Locator("table.inventory-table tbody tr").Filter(new() { HasText = itemName }).First;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var count = await itemRow.CountAsync();

            if (shouldExist)
            {
                if (count > 0 && await itemRow.IsVisibleAsync())
                    return itemRow;
            }
            else
            {
                if (count == 0)
                    return itemRow;
            }

            await Page.WaitForTimeoutAsync(1000);
            await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForInventoryManagementPageReady();
            await FilterInventoryBySearchTerm(itemName);
        }

        if (shouldExist)
            throw new Exception($"Inventory management row for '{itemName}' did not appear within {timeoutMs}ms.");

        throw new Exception($"Inventory management row for '{itemName}' was still present after {timeoutMs}ms.");
    }
}
