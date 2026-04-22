using Microsoft.Playwright;
using PlaywrightTests.Tests.Base;
using PlaywrightTests.Tests.Setup;

namespace PlaywrightTests.Tests.ActualTests.AdminTests;

[Collection("Admin Auth Collection")]
public class RecipeTests : AuthenticatedTestBase
{
    private const string RecipesUrl = "http://localhost:5256/recipes";
    private const string InventoryCatalogUrl = "http://localhost:5256/inventory-catalog";
    private const int DefaultUiTimeout = 10000;
    private const int LongUiTimeout = 20000;

    [Fact]
    public async Task Admin_Recipe_Lifecycle_Create_Edit_ProduceValidation_Delete()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var ingredientName = $"PW Ingredient {unique}";
        var ingredientSku = $"PW-SKU-{unique}";
        var recipeName = $"PW Recipe {unique}";
        var editedRecipeName = $"PW Recipe Edited {unique}";

        try
        {
            await CreateIngredientIfMissing(ingredientName, ingredientSku);
            await CreateRecipe(recipeName, ingredientName);
            await VerifyRecipeExists(recipeName);
            await PauseForPhotoAsync("recipe created and visible in the recipe list");

            await EditRecipeTitle(recipeName, editedRecipeName);
            await VerifyRecipeExists(editedRecipeName);
            await PauseForPhotoAsync("edited recipe visible in the recipe list");

            await OpenRecipeAndVerifyProduceValidation(editedRecipeName);

            await DeleteRecipe(editedRecipeName);
            await VerifyRecipeDeleted(editedRecipeName);
        }
        finally
        {
            await DeleteRecipeIfExists(editedRecipeName);
            await DeleteRecipeIfExists(recipeName);
            await DeleteIngredientIfExists(ingredientName);
        }
    }

    [Fact]
    public async Task Admin_Should_Create_SubRecipe_And_Filter_On_SubRecipe_Tab()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var ingredientName = $"PW Ingredient {unique}";
        var ingredientSku = $"PW-SKU-{unique}";
        var subRecipeName = $"PW SubRecipe {unique}";

        try
        {
            await CreateIngredientIfMissing(ingredientName, ingredientSku);
            await CreateSubRecipe(subRecipeName, ingredientName);
            await VerifyRecipeExists(subRecipeName);

            await GoToRecipesPage();
            await Page.GetByRole(AriaRole.Button, new() { Name = "Sub-Recipes / Prep Items" }).ClickAsync();

            await Page.GetByPlaceholder("Search recipes…").FillAsync(subRecipeName);
            await Expect(Page.GetByText(subRecipeName)).ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        }
        finally
        {
            await DeleteRecipeIfExists(subRecipeName);
            await DeleteIngredientIfExists(ingredientName);
        }
    }

    private async Task CreateIngredientIfMissing(string ingredientName, string ingredientSku)
    {
        await GoToInventoryCatalogPage();
        await DismissBlockingUiIfPresent();

        await Page.GetByPlaceholder("Search catalog by name, category, or supplier...")
            .FillAsync(ingredientName);

        var existingRow = Page.Locator("tr.data-row").Filter(new() { HasText = ingredientName }).First;
        if (await existingRow.CountAsync() > 0 && await existingRow.IsVisibleAsync())
            return;

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Item" }).ClickAsync();

        var modal = Page.Locator(".modal-dialog-custom").Last;
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Add New Item" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        var inputs = modal.Locator("input.form-control");
        var selects = modal.Locator("select.form-control");

        await Expect(inputs).ToHaveCountAsync(5, new() { Timeout = DefaultUiTimeout });
        await Expect(selects).ToHaveCountAsync(4, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(0).FillAsync(ingredientName);   // Item Name
        await inputs.Nth(1).FillAsync(ingredientSku);    // SKU
        await selects.Nth(1).SelectOptionAsync(new SelectOptionValue { Label = "Produce" }); // Category
        await selects.Nth(3).SelectOptionAsync(new SelectOptionValue { Index = 1 });          // Unit
        await inputs.Nth(2).FillAsync("2.50");           // Cost Per Unit
        await inputs.Nth(3).FillAsync("25");             // Starting Quantity
        await inputs.Nth(4).FillAsync("5");              // Reorder Level

        var addItemButton = modal.GetByRole(AriaRole.Button, new() { Name = "Add Item" });
        await Expect(addItemButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        // Click via JS because the modal footer button sits outside the visible viewport region
        await addItemButton.EvaluateAsync("button => button.click()");


        await Page.WaitForTimeoutAsync(1000);
        await GoToInventoryCatalogPage();

        await Page.GetByPlaceholder("Search catalog by name, category, or supplier...")
            .FillAsync(ingredientName);

        var ingredientRow = await WaitForInventoryItemRow(ingredientName, shouldExist: true, timeoutMs: LongUiTimeout);
        await Expect(ingredientRow).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task CreateRecipe(string recipeName, string ingredientName)
    {
        await Page.GotoAsync("http://localhost:5256/recipes/create", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForRecipeFormReady("New Recipe");

        var titleInput = Page.GetByPlaceholder("e.g. Caesar Salad");
        var topInputs = Page.Locator("div.card-body input.form-control");
        var topSelects = Page.Locator("div.card-body select.form-select");

        await titleInput.FillAsync(recipeName);

        // Top-level selects:
        // 0 = Category
        // 1 = Recipe Type
        // 2 = Yield Unit
        await topSelects.Nth(0).SelectOptionAsync(new SelectOptionValue { Label = "Prepared" });
        await topSelects.Nth(1).SelectOptionAsync(new SelectOptionValue { Index = 1 });

        // Top-level numeric input after title is Yield Amount
        await topInputs.Nth(1).FillAsync("4");

        await topSelects.Nth(2).SelectOptionAsync(new SelectOptionValue { Index = 1 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Line" }).ClickAsync();

        var smallSelects = Page.Locator("select.form-select.form-select-sm");
        await Expect(smallSelects).ToHaveCountAsync(3, new() { Timeout = DefaultUiTimeout });

        await smallSelects.Nth(0).SelectOptionAsync(new SelectOptionValue { Label = "Produce" });
        await smallSelects.Nth(1).SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await smallSelects.Nth(2).SelectOptionAsync(new SelectOptionValue { Index = 1 });

        var numberInputs = Page.Locator("input[type='number']");
        await numberInputs.Nth(1).FillAsync("2");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Step" }).ClickAsync();
        await Page.Locator("textarea").First.FillAsync("Prepare the recipe.");

        var saveButton = Page.GetByRole(AriaRole.Button, new() { Name = "Save Recipe" });
        await Expect(saveButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await saveButton.ClickAsync();

        await Page.WaitForURLAsync("**/recipes", new() { Timeout = LongUiTimeout });
        await WaitForRecipesPageReady();
    }

    private async Task CreateSubRecipe(string subRecipeName, string ingredientName)
    {
        await Page.GotoAsync("http://localhost:5256/recipes/create", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForRecipeFormReady("New Recipe");

        var titleInput = Page.GetByPlaceholder("e.g. Caesar Salad");
        var topInputs = Page.Locator("div.card-body input.form-control");
        var topSelects = Page.Locator("div.card-body select.form-select");

        await titleInput.FillAsync(subRecipeName);

        // 0 = Category
        // 1 = Recipe Type
        // 2 = Yield Unit
        await topSelects.Nth(0).SelectOptionAsync(new SelectOptionValue { Label = "Prepared" });
        await topSelects.Nth(1).SelectOptionAsync(new SelectOptionValue { Index = 1 });

        await topInputs.Nth(1).FillAsync("1");

        await topSelects.Nth(2).SelectOptionAsync(new SelectOptionValue { Index = 1 });

        // Toggle sub-recipe checkbox by id instead of label text
        await Page.Locator("#isSubRecipeToggle").CheckAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Line" }).ClickAsync();

        var smallSelects = Page.Locator("select.form-select.form-select-sm");
        await Expect(smallSelects).ToHaveCountAsync(3, new() { Timeout = DefaultUiTimeout });

        await smallSelects.Nth(0).SelectOptionAsync(new SelectOptionValue { Label = "Produce" });
        await smallSelects.Nth(1).SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
        await smallSelects.Nth(2).SelectOptionAsync(new SelectOptionValue { Index = 1 });

        var numberInputs = Page.Locator("input[type='number']");
        await numberInputs.Nth(1).FillAsync("1");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Step" }).ClickAsync();
        await Page.Locator("textarea").First.FillAsync("Prep item step.");

        var saveButton = Page.GetByRole(AriaRole.Button, new() { Name = "Save Recipe" });
        await Expect(saveButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await saveButton.ClickAsync();

        await Page.WaitForURLAsync("**/recipes", new() { Timeout = LongUiTimeout });
        await WaitForRecipesPageReady();
    }

    private async Task EditRecipeTitle(string currentName, string newName)
    {
        await GoToRecipesPage();

        await Page.GetByPlaceholder("Search recipes…").FillAsync(currentName);

        var recipeRow = await WaitForRecipeRow(currentName, shouldExist: true, timeoutMs: LongUiTimeout);

        var editButton = recipeRow.GetByTitle("Edit").First;
        await Expect(editButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await editButton.ClickAsync();

        await Page.WaitForURLAsync(url => url.Contains("/recipes/edit/"), new() { Timeout = LongUiTimeout });
        await WaitForRecipeFormReady("Edit Recipe");

        var titleInput = Page.GetByPlaceholder("e.g. Caesar Salad");
        await titleInput.FillAsync(newName);

        var saveButton = Page.GetByRole(AriaRole.Button, new() { Name = "Save Recipe" });
        await Expect(saveButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await saveButton.ClickAsync();

        await Page.WaitForURLAsync("**/recipes", new() { Timeout = LongUiTimeout });
        await WaitForRecipesPageReady();
    }

    private async Task VerifyRecipeExists(string recipeName)
    {
        await GoToRecipesPage();
        await DismissBlockingUiIfPresent();

        await Page.GetByPlaceholder("Search recipes…").FillAsync(recipeName);

        var recipeRow = await WaitForRecipeRow(recipeName, shouldExist: true, timeoutMs: LongUiTimeout);
        await Expect(recipeRow).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }


    private async Task OpenRecipeAndVerifyProduceValidation(string recipeName)
    {
        await GoToRecipesPage();
        await DismissBlockingUiIfPresent();

        await Page.GetByPlaceholder("Search recipes…").FillAsync(recipeName);

        var recipeRow = await WaitForRecipeRow(recipeName, shouldExist: true, timeoutMs: LongUiTimeout);

        var viewButton = recipeRow.GetByTitle("View").First;
        await Expect(viewButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await viewButton.ClickAsync();

        await Page.WaitForURLAsync(url => url.Contains("/recipes/view/"), new() { Timeout = LongUiTimeout });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await PauseForPhotoAsync("recipe detail page before opening the Produce modal");

        var openProduceButton = Page.GetByRole(AriaRole.Button, new() { Name = "Produce" }).First;
        await Expect(openProduceButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await openProduceButton.ClickAsync();

        var modal = await WaitForOpenDialog("Produce Recipe");
        await Expect(modal).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await PauseForPhotoAsync("Produce Recipe modal open");

        var servingsInput = modal.Locator("input[type='number']").First;
        await servingsInput.FillAsync("0");
        await servingsInput.PressAsync("Tab");
        await Page.WaitForTimeoutAsync(300);
        await PauseForPhotoAsync("Produce Recipe modal showing zero-serving validation state");

        var isValid = await servingsInput.EvaluateAsync<bool>("input => input.checkValidity()");
        var validationMessage = await servingsInput.EvaluateAsync<string>("input => input.validationMessage");

        Assert.False(isValid, "Expected servings input to reject a value of 0.");
        Assert.False(string.IsNullOrWhiteSpace(validationMessage),
            "Expected servings input to provide a validation message for a value of 0.");

        await CloseDialog(modal, requireClosed: false);
    }

    private async Task DeleteRecipe(string recipeName)
    {
        await GoToRecipesPage();
        await DismissBlockingUiIfPresent();

        await Page.GetByPlaceholder("Search recipes…").FillAsync(recipeName);

        var recipeRow = await WaitForRecipeRow(recipeName, shouldExist: true, timeoutMs: LongUiTimeout);

        var deleteButton = recipeRow.GetByTitle("Delete").First;
        await Expect(deleteButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await deleteButton.ClickAsync();

        var deleteModal = await WaitForOpenDialog("Delete Recipe");
        var confirmDeleteButton = deleteModal.GetByRole(AriaRole.Button, new() { Name = "Delete" }).First;
        await Expect(confirmDeleteButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await confirmDeleteButton.EvaluateAsync("button => button.click()");

        await Page.WaitForTimeoutAsync(1000);
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitForRecipesPageReady();
    }

    private async Task VerifyRecipeDeleted(string recipeName)
    {
        await WaitForRecipeRow(recipeName, shouldExist: false, timeoutMs: LongUiTimeout);
    }

    private async Task DeleteRecipeIfExists(string recipeName)
    {
        await GoToRecipesPage();
        await DismissBlockingUiIfPresent();

        await Page.GetByPlaceholder("Search recipes…").FillAsync(recipeName);

        var recipeRow = Page.Locator("table.recipes-table tbody tr").Filter(new() { HasText = recipeName }).First;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var count = await recipeRow.CountAsync();
            if (count == 0)
                return;

            if (await recipeRow.IsVisibleAsync())
            {
                await DismissBlockingUiIfPresent();

                var deleteButton = recipeRow.GetByTitle("Delete").First;
                if (await deleteButton.IsVisibleAsync())
                {
                    await deleteButton.ClickAsync();

                    var deleteModal = await WaitForOpenDialog("Delete Recipe");
                    var confirmDeleteButton = deleteModal.GetByRole(AriaRole.Button, new() { Name = "Delete" }).First;
                    if (await confirmDeleteButton.IsVisibleAsync())
                    {
                        await confirmDeleteButton.EvaluateAsync("button => button.click()");
                        await Page.WaitForTimeoutAsync(1000);
                        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                        await WaitForRecipesPageReady();
                    }
                }
            }

            await Page.WaitForTimeoutAsync(1000);
        }
    }

    private async Task DeleteIngredientIfExists(string ingredientName)
    {
        await GoToInventoryCatalogPage();
        await DismissBlockingUiIfPresent();

        await Page.GetByPlaceholder("Search catalog by name, category, or supplier...")
            .FillAsync(ingredientName);

        var itemRow = Page.Locator("tr.data-row").Filter(new() { HasText = ingredientName }).First;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var count = await itemRow.CountAsync();
            if (count == 0)
                return;

            if (await itemRow.IsVisibleAsync())
            {
                await DismissBlockingUiIfPresent();

                var menuButton = itemRow.Locator(".ellipsis-btn").First;
                await menuButton.ClickAsync();

                var deleteButton = Page.GetByRole(AriaRole.Button, new() { Name = "Delete Item" }).First;
                if (await deleteButton.IsVisibleAsync())
                {
                    await deleteButton.ClickAsync();
                    await Page.WaitForTimeoutAsync(1000);
                    await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    await WaitForInventoryCatalogPageReady();
                }
            }

            await Page.WaitForTimeoutAsync(1000);
        }
    }

    private async Task GoToRecipesPage()
    {
        await DismissBlockingUiIfPresent();
        await Page.GotoAsync(RecipesUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForRecipesPageReady();
    }

    private async Task GoToInventoryCatalogPage()
    {
        await DismissBlockingUiIfPresent();
        await Page.GotoAsync(InventoryCatalogUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForInventoryCatalogPageReady();
    }

    private async Task WaitForRecipesPageReady()
    {
        await Page.WaitForURLAsync("**/recipes", new() { Timeout = LongUiTimeout });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Recipes" }))
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

    private async Task<ILocator> WaitForInventoryItemRow(string ingredientName, bool shouldExist, int timeoutMs = LongUiTimeout)
    {
        var itemRow = Page.Locator("tr.data-row").Filter(new() { HasText = ingredientName }).First;
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
            await WaitForInventoryCatalogPageReady();

            await Page.GetByPlaceholder("Search catalog by name, category, or supplier...")
                .FillAsync(ingredientName);
        }

        if (shouldExist)
            throw new Exception($"Inventory row for '{ingredientName}' did not appear within {timeoutMs}ms.");

        throw new Exception($"Inventory row for '{ingredientName}' was still present after {timeoutMs}ms.");
    }

    private async Task WaitForRecipeFormReady(string formTitle)
    {
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = formTitle }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        var titleInput = Page.GetByPlaceholder("e.g. Caesar Salad");
        await Expect(titleInput).ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        await Page.WaitForTimeoutAsync(500);
    }

    private async Task<ILocator> WaitForRecipeRow(string recipeName, bool shouldExist, int timeoutMs = LongUiTimeout)
    {
        var recipeRow = Page.Locator("table.recipes-table tbody tr").Filter(new() { HasText = recipeName }).First;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var count = await recipeRow.CountAsync();

            if (shouldExist)
            {
                if (count > 0 && await recipeRow.IsVisibleAsync())
                    return recipeRow;
            }
            else
            {
                if (count == 0)
                    return recipeRow;
            }

            await Page.WaitForTimeoutAsync(1000);
            await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForRecipesPageReady();

            await Page.GetByPlaceholder("Search recipes…").FillAsync(recipeName);
        }

        if (shouldExist)
            throw new Exception($"Recipe row for '{recipeName}' did not appear within {timeoutMs}ms.");

        throw new Exception($"Recipe row for '{recipeName}' was still present after {timeoutMs}ms.");
    }

    private async Task<ILocator> WaitForOpenDialog(string headingText)
    {
        var dialog = Page.Locator("div.modal.fade.show.d-block[role='dialog']")
            .Filter(new()
            {
                Has = Page.GetByRole(AriaRole.Heading, new() { Name = headingText })
            })
            .Last;

        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await Expect(dialog.GetByRole(AriaRole.Heading, new() { Name = headingText }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        return dialog;
    }

    private async Task CloseDialog(ILocator dialog, bool requireClosed = true)
    {
        var cancelButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).First;
        if (await cancelButton.CountAsync() > 0 && await cancelButton.IsVisibleAsync())
        {
            await cancelButton.EvaluateAsync("button => button.click()");
        }
        else
        {
            var closeButton = dialog.Locator(".btn-close").First;
            if (await closeButton.CountAsync() > 0 && await closeButton.IsVisibleAsync())
                await closeButton.EvaluateAsync("button => button.click()");
        }

        if (!requireClosed)
        {
            await Page.WaitForTimeoutAsync(300);
            return;
        }

        await Expect(dialog).Not.ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task DismissBlockingUiIfPresent()
    {
        var rowMenuBackdrop = Page.Locator(".row-menu-backdrop").First;
        if (await rowMenuBackdrop.CountAsync() > 0 && await rowMenuBackdrop.IsVisibleAsync())
        {
            await rowMenuBackdrop.ClickAsync(new() { Force = true });
            await Page.WaitForTimeoutAsync(200);
        }

        var bootstrapDialogs = Page.Locator("div.modal.fade.show.d-block[role='dialog']");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (await bootstrapDialogs.CountAsync() == 0)
                break;

            var dialog = bootstrapDialogs.Last;
            if (!await dialog.IsVisibleAsync())
                break;

            await CloseDialog(dialog, requireClosed: false);
            await Page.WaitForTimeoutAsync(200);
        }

        var customModal = Page.Locator(".modal-dialog-custom").Last;
        if (await customModal.CountAsync() > 0 && await customModal.IsVisibleAsync())
        {
            var closeButton = customModal.Locator(".btn-close-custom").First;
            if (await closeButton.CountAsync() > 0 && await closeButton.IsVisibleAsync())
            {
                await closeButton.EvaluateAsync("button => button.click()");
                await Page.WaitForTimeoutAsync(200);
            }
        }
    }
}
