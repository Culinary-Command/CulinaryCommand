using Microsoft.Playwright;
using PlaywrightTests.Tests.Base;

namespace PlaywrightTests.Tests.ActualTests.AdminTests;

[Collection("Admin Auth Collection")]
public class TaskManagementTests : AuthenticatedTestBase
{
    private const string AssignTasksUrl = "http://localhost:5256/assign-tasks";
    private const int DefaultUiTimeout = 10000;
    private const int LongUiTimeout = 20000;

    [Fact]
    public async Task Admin_TaskManagement_ManualTask_Lifecycle_Create_Start_Complete_Delete()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var taskName = $"PW Manual Task {unique}";
        var notes = $"Created by Playwright {unique}";

        try
        {
            await CreateManualTask(taskName, station: "Dish", priority: "High", notes);
            await VerifyTaskInStatus(taskName, "Pending");
            await PauseForPhotoAsync("manual task visible in Pending");

            await MoveTaskToInProgress(taskName);
            await VerifyTaskInStatus(taskName, "In Progress");
            await PauseForPhotoAsync("manual task moved to In Progress");

            await MarkTaskDone(taskName);
            await VerifyTaskInStatus(taskName, "Completed");
            await PauseForPhotoAsync("manual task visible in Completed");

            await DeleteTask(taskName);
            await VerifyTaskDeleted(taskName);
        }
        finally
        {
            await DeleteTaskIfExists(taskName);
        }
    }

    [Fact]
    public async Task Admin_TaskManagement_Template_QuickAssign_Create_Assign_Delete()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var templateName = $"PW Template Task {unique}";
        var notes = $"Template notes {unique}";

        try
        {
            await CreateTaskTemplate(templateName, station: "Prep", priority: "Critical", notes);
            await VerifyTemplateExists(templateName);
            await PauseForPhotoAsync("task template visible in library");

            await SelectTemplateForQuickAssign(templateName);
            await AssignSelectedTemplates(priorityOverride: "High");

            await VerifyTaskInStatus(templateName, "Pending");
            await Expect(GetTaskCard("Pending", templateName)).ToContainTextAsync("High", new() { Timeout = DefaultUiTimeout });
            await PauseForPhotoAsync("quick-assigned template visible as pending task");

            await DeleteTask(templateName);
            await ArchiveTemplate(templateName);

            await VerifyTaskDeleted(templateName);
            await VerifyTemplateDeleted(templateName);
        }
        finally
        {
            await DeleteTaskIfExists(templateName);
            await ArchiveTemplateIfExists(templateName);
        }
    }

    [Fact]
    public async Task Admin_TaskManagement_TaskList_Create_Assign_Delete()
    {
        var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var templateName = $"PW List Template {unique}";
        var taskListName = $"PW Task List {unique}";
        var taskListNotes = $"Task list notes {unique}";

        try
        {
            await CreateTaskTemplate(templateName, station: "Expo", priority: "Normal", notes: $"List template notes {unique}");
            await CreateTaskList(taskListName, taskListNotes, templateName);
            await VerifyTaskListExists(taskListName);
            await PauseForPhotoAsync("task list visible with selected template");

            await SelectTaskListForQuickAssign(taskListName);
            await AssignSelectedTemplates(priorityOverride: "Keep Original");

            await VerifyTaskInStatus(templateName, "Pending");
            await PauseForPhotoAsync("task created from task list assignment");

            await DeleteTask(templateName);
            await ArchiveTaskList(taskListName);
            await ArchiveTemplate(templateName);

            await VerifyTaskDeleted(templateName);
            await VerifyTaskListDeleted(taskListName);
            await VerifyTemplateDeleted(templateName);
        }
        finally
        {
            await DeleteTaskIfExists(templateName);
            await ArchiveTaskListIfExists(taskListName);
            await ArchiveTemplateIfExists(templateName);
        }
    }

    private async Task CreateManualTask(string taskName, string station, string priority, string notes)
    {
        await GoToTaskAssignmentPage();

        var form = Page.Locator("details").Filter(new() { HasText = "Create Task Manually" }).First;
        await Expect(form).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        if (await form.GetAttributeAsync("open") is null)
        {
            await form.Locator("summary").ClickAsync();
        }

        var inputs = form.Locator("input.form-control");
        var selects = form.Locator("select.form-select");
        var notesInput = form.Locator("textarea.form-control").First;

        await Expect(inputs.Nth(0)).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await Expect(selects).ToHaveCountAsync(4, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(0).FillAsync(taskName);
        await selects.Nth(1).SelectOptionAsync(new SelectOptionValue { Label = station });
        await selects.Nth(2).SelectOptionAsync(new SelectOptionValue { Label = priority });
        await notesInput.FillAsync(notes);

        var assignButton = form.GetByRole(AriaRole.Button, new() { Name = "Assign Task" });
        await Expect(assignButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await assignButton.ClickAsync();

        await WaitForTaskInAnyStatus(taskName, shouldExist: true, timeoutMs: LongUiTimeout);
    }

    private async Task CreateTaskTemplate(string templateName, string station, string priority, string notes)
    {
        await GoToTaskAssignmentPage();

        await GetTaskLibraryPanel()
            .GetByRole(AriaRole.Button, new() { Name = "+ New Template" })
            .ClickAsync();

        var modal = Page.Locator(".template-modal").Last;
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Create Task Template" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        var inputs = modal.Locator("input.form-control");
        var selects = modal.Locator("select.form-select");

        await Expect(inputs.Nth(0)).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await Expect(selects).ToHaveCountAsync(3, new() { Timeout = DefaultUiTimeout });

        await inputs.Nth(0).FillAsync(templateName);
        await selects.Nth(0).SelectOptionAsync(new SelectOptionValue { Label = station });
        await selects.Nth(2).SelectOptionAsync(new SelectOptionValue { Label = priority });
        await modal.Locator("input[type='number']").First.FillAsync("15");
        await modal.Locator("textarea").First.FillAsync(notes);

        var createButton = modal.GetByRole(AriaRole.Button, new() { Name = "Create Template" });
        await Expect(createButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await createButton.ClickAsync();

        await Expect(Page.GetByText($"Template '{templateName}' created successfully."))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await Expect(modal).ToBeHiddenAsync(new() { Timeout = LongUiTimeout });
    }

    private async Task CreateTaskList(string taskListName, string notes, string templateName)
    {
        await GoToTaskAssignmentPage();

        await GetTaskListsPanel()
            .GetByRole(AriaRole.Button, new() { Name = "+ New List" })
            .ClickAsync();

        var modal = Page.Locator(".task-list-modal").Last;
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Create Task List" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        await modal.Locator("input.form-control").First.FillAsync(taskListName);
        await modal.Locator("textarea.form-control").First.FillAsync(notes);
        await modal.GetByPlaceholder("Search templates...").FillAsync(templateName);

        var templateOption = modal.Locator(".task-list-template-item").Filter(new() { HasText = templateName }).First;
        await Expect(templateOption).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await templateOption.ClickAsync();

        var createButton = modal.GetByRole(AriaRole.Button, new() { Name = "Create Task List" });
        await Expect(createButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await createButton.ClickAsync();

        await Expect(Page.GetByText($"Task list '{taskListName}' created successfully."))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await Expect(modal).ToBeHiddenAsync(new() { Timeout = LongUiTimeout });
    }

    private async Task SelectTemplateForQuickAssign(string templateName)
    {
        await GoToTaskAssignmentPage();
        await FilterTemplateLibraryBySearchTerm(templateName);

        var templateCard = await WaitForTemplateCard(templateName, shouldExist: true, timeoutMs: LongUiTimeout);
        var checkbox = templateCard.Locator("input[type='checkbox']").First;

        await Expect(checkbox).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await checkbox.CheckAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Quick Assign" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task SelectTaskListForQuickAssign(string taskListName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskListsBySearchTerm(taskListName);

        var taskListCard = await WaitForTaskListCard(taskListName, shouldExist: true, timeoutMs: LongUiTimeout);
        var assignListButton = taskListCard.GetByRole(AriaRole.Button, new() { Name = "Assign List" });

        await Expect(assignListButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await assignListButton.ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Quick Assign" }))
            .ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task AssignSelectedTemplates(string priorityOverride)
    {
        var quickAssignPanel = GetQuickAssignPanel();
        await Expect(quickAssignPanel).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        var prioritySelect = quickAssignPanel.Locator("select.form-select").Nth(1);
        await Expect(prioritySelect).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await prioritySelect.SelectOptionAsync(new SelectOptionValue { Label = priorityOverride });

        var assignButton = quickAssignPanel.GetByRole(AriaRole.Button, new() { Name = "Assign Selected Tasks" });
        await Expect(assignButton).ToBeEnabledAsync(new() { Timeout = DefaultUiTimeout });
        await assignButton.ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Quick Assign" }))
            .ToBeHiddenAsync(new() { Timeout = LongUiTimeout });
    }

    private async Task MoveTaskToInProgress(string taskName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskBoardBySearchTerm(taskName);

        var taskCard = await WaitForTaskCard("Pending", taskName, shouldExist: true, timeoutMs: LongUiTimeout);
        var startButton = taskCard.GetByRole(AriaRole.Button, new() { Name = "Start" });

        await Expect(startButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await startButton.ClickAsync();
    }

    private async Task MarkTaskDone(string taskName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskBoardBySearchTerm(taskName);

        var taskCard = await WaitForTaskCard("In Progress", taskName, shouldExist: true, timeoutMs: LongUiTimeout);
        var doneButton = taskCard.GetByRole(AriaRole.Button, new() { Name = "Mark done" });

        await Expect(doneButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await doneButton.ClickAsync();
    }

    private async Task DeleteTask(string taskName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskBoardBySearchTerm(taskName);

        var taskCard = await WaitForTaskInAnyStatus(taskName, shouldExist: true, timeoutMs: LongUiTimeout);
        await ClickCardDropdownAction(taskCard, "Remove");

        await WaitForTaskInAnyStatus(taskName, shouldExist: false, timeoutMs: LongUiTimeout);
    }

    private async Task ArchiveTemplate(string templateName)
    {
        await GoToTaskAssignmentPage();
        await FilterTemplateLibraryBySearchTerm(templateName);

        var templateCard = await WaitForTemplateCard(templateName, shouldExist: true, timeoutMs: LongUiTimeout);
        await ClickCardDropdownAction(templateCard, "Delete", expectsConfirm: true);

        await Expect(Page.GetByText("Template removed successfully."))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
    }

    private async Task ArchiveTaskList(string taskListName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskListsBySearchTerm(taskListName);

        var taskListCard = await WaitForTaskListCard(taskListName, shouldExist: true, timeoutMs: LongUiTimeout);
        await ClickCardDropdownAction(taskListCard, "Delete", expectsConfirm: true);

        await Expect(Page.GetByText("Task list removed successfully."))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
    }

    private async Task DeleteTaskIfExists(string taskName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskBoardBySearchTerm(taskName);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var taskCard = GetTaskCardInAnyStatus(taskName);
            if (await taskCard.CountAsync() == 0)
                return;

            if (await taskCard.IsVisibleAsync())
            {
                await ClickCardDropdownAction(taskCard, "Remove");
                await Page.WaitForTimeoutAsync(500);
            }

            await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForTaskAssignmentPageReady();
            await FilterTaskBoardBySearchTerm(taskName);
        }
    }

    private async Task ArchiveTemplateIfExists(string templateName)
    {
        await GoToTaskAssignmentPage();
        await FilterTemplateLibraryBySearchTerm(templateName);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var templateCard = GetTemplateCard(templateName);
            if (await templateCard.CountAsync() == 0)
                return;

            if (await templateCard.IsVisibleAsync())
            {
                await ClickCardDropdownAction(templateCard, "Delete", expectsConfirm: true);
                await Page.WaitForTimeoutAsync(500);
            }

            await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForTaskAssignmentPageReady();
            await FilterTemplateLibraryBySearchTerm(templateName);
        }
    }

    private async Task ArchiveTaskListIfExists(string taskListName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskListsBySearchTerm(taskListName);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var taskListCard = GetTaskListCard(taskListName);
            if (await taskListCard.CountAsync() == 0)
                return;

            if (await taskListCard.IsVisibleAsync())
            {
                await ClickCardDropdownAction(taskListCard, "Delete", expectsConfirm: true);
                await Page.WaitForTimeoutAsync(500);
            }

            await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForTaskAssignmentPageReady();
            await FilterTaskListsBySearchTerm(taskListName);
        }
    }

    private async Task VerifyTaskInStatus(string taskName, string status)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskBoardBySearchTerm(taskName);

        var taskCard = await WaitForTaskCard(status, taskName, shouldExist: true, timeoutMs: LongUiTimeout);
        await Expect(taskCard).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task VerifyTaskDeleted(string taskName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskBoardBySearchTerm(taskName);
        await WaitForTaskInAnyStatus(taskName, shouldExist: false, timeoutMs: LongUiTimeout);
    }

    private async Task VerifyTemplateExists(string templateName)
    {
        await GoToTaskAssignmentPage();
        await FilterTemplateLibraryBySearchTerm(templateName);

        var templateCard = await WaitForTemplateCard(templateName, shouldExist: true, timeoutMs: LongUiTimeout);
        await Expect(templateCard).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task VerifyTemplateDeleted(string templateName)
    {
        await GoToTaskAssignmentPage();
        await FilterTemplateLibraryBySearchTerm(templateName);
        await WaitForTemplateCard(templateName, shouldExist: false, timeoutMs: LongUiTimeout);
    }

    private async Task VerifyTaskListExists(string taskListName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskListsBySearchTerm(taskListName);

        var taskListCard = await WaitForTaskListCard(taskListName, shouldExist: true, timeoutMs: LongUiTimeout);
        await Expect(taskListCard).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
    }

    private async Task VerifyTaskListDeleted(string taskListName)
    {
        await GoToTaskAssignmentPage();
        await FilterTaskListsBySearchTerm(taskListName);
        await WaitForTaskListCard(taskListName, shouldExist: false, timeoutMs: LongUiTimeout);
    }

    private async Task GoToTaskAssignmentPage()
    {
        if (await IsTaskAssignmentPageVisible())
            return;

        await Page.GotoAsync(AssignTasksUrl, new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await WaitForTaskAssignmentPageReady();
    }

    private async Task<bool> IsTaskAssignmentPageVisible()
    {
        if (!Page.Url.Contains("/assign-tasks", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            await Page.GetByRole(AriaRole.Heading, new() { Name = "Task Assignment" })
                .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 1000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task WaitForTaskAssignmentPageReady()
    {
        await Page.WaitForURLAsync("**/assign-tasks", new() { Timeout = LongUiTimeout });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Task Assignment" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Task Lists" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Task Library" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Kitchen Task Board" }))
            .ToBeVisibleAsync(new() { Timeout = LongUiTimeout });

        await Page.WaitForTimeoutAsync(500);
    }

    private async Task FilterTaskBoardBySearchTerm(string searchTerm)
    {
        var searchInput = Page.GetByPlaceholder("Search tasks or people");
        await Expect(searchInput).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await searchInput.FillAsync(searchTerm);
        await Page.WaitForTimeoutAsync(500);
    }

    private async Task FilterTemplateLibraryBySearchTerm(string searchTerm)
    {
        var searchInput = GetTaskLibraryPanel().GetByPlaceholder("Search templates...");
        await Expect(searchInput).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await searchInput.FillAsync(searchTerm);
        await Page.WaitForTimeoutAsync(500);
    }

    private async Task FilterTaskListsBySearchTerm(string searchTerm)
    {
        var searchInput = GetTaskListsPanel().GetByPlaceholder("Search lists...");
        await Expect(searchInput).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await searchInput.FillAsync(searchTerm);
        await Page.WaitForTimeoutAsync(500);
    }

    private ILocator GetTaskLibraryPanel() =>
        Page.Locator(".card").Filter(new() { HasText = "Task Library" }).First;

    private ILocator GetTaskListsPanel() =>
        Page.Locator(".card").Filter(new() { HasText = "Task Lists" }).First;

    private ILocator GetQuickAssignPanel() =>
        Page.Locator(".card").Filter(new() { HasText = "Quick Assign" }).First;

    private ILocator GetStatusColumn(string status) =>
        Page.Locator(".col-12.col-lg-4").Filter(new() { HasText = status }).First;

    private ILocator GetTaskCard(string status, string taskName) =>
        GetStatusColumn(status).Locator(".assign-item-card").Filter(new() { HasText = taskName }).First;

    private ILocator GetTaskCardInAnyStatus(string taskName) =>
        Page.Locator(".col-12.col-lg-4 .assign-item-card").Filter(new() { HasText = taskName }).First;

    private ILocator GetTemplateCard(string templateName) =>
        GetTaskLibraryPanel().Locator(".assign-item-card").Filter(new() { HasText = templateName }).First;

    private ILocator GetTaskListCard(string taskListName) =>
        GetTaskListsPanel().Locator(".assign-item-card").Filter(new() { HasText = taskListName }).First;

    private async Task<ILocator> WaitForTaskCard(string status, string taskName, bool shouldExist, int timeoutMs)
    {
        var taskCard = GetTaskCard(status, taskName);
        return await WaitForCard(taskCard, $"task '{taskName}' in status '{status}'", shouldExist, timeoutMs);
    }

    private async Task<ILocator> WaitForTaskInAnyStatus(string taskName, bool shouldExist, int timeoutMs)
    {
        var taskCard = GetTaskCardInAnyStatus(taskName);
        return await WaitForCard(taskCard, $"task '{taskName}'", shouldExist, timeoutMs);
    }

    private async Task<ILocator> WaitForTemplateCard(string templateName, bool shouldExist, int timeoutMs)
    {
        var templateCard = GetTemplateCard(templateName);
        return await WaitForCard(templateCard, $"template '{templateName}'", shouldExist, timeoutMs);
    }

    private async Task<ILocator> WaitForTaskListCard(string taskListName, bool shouldExist, int timeoutMs)
    {
        var taskListCard = GetTaskListCard(taskListName);
        return await WaitForCard(taskListCard, $"task list '{taskListName}'", shouldExist, timeoutMs);
    }

    private async Task<ILocator> WaitForCard(ILocator card, string label, bool shouldExist, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var count = await card.CountAsync();

            if (shouldExist)
            {
                if (count > 0 && await card.IsVisibleAsync())
                    return card;
            }
            else
            {
                if (count == 0 || !await card.IsVisibleAsync())
                    return card;
            }

            await Page.WaitForTimeoutAsync(500);
        }

        if (shouldExist)
            throw new Exception($"Expected {label} to appear within {timeoutMs}ms.");

        throw new Exception($"Expected {label} to be removed within {timeoutMs}ms.");
    }

    private async Task ClickCardDropdownAction(ILocator card, string actionName, bool expectsConfirm = false)
    {
        var manageButton = card.GetByRole(AriaRole.Button, new() { Name = "Manage" }).First;
        await Expect(manageButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });
        await manageButton.ClickAsync();

        var actionButton = card.GetByRole(AriaRole.Button, new() { Name = actionName }).First;
        await Expect(actionButton).ToBeVisibleAsync(new() { Timeout = DefaultUiTimeout });

        if (expectsConfirm)
        {
            await HandleConfirmDialogAsync(actionButton, accept: true);
        }
        else
        {
            await actionButton.ClickAsync();
        }
    }

    private async Task HandleConfirmDialogAsync(ILocator actionButton, bool accept)
    {
        var dialogTcs = new TaskCompletionSource<IDialog>();
        void Handler(object? _, IDialog dialog) => dialogTcs.TrySetResult(dialog);

        Page.Dialog += Handler;
        try
        {
            await actionButton.ClickAsync();

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
}
