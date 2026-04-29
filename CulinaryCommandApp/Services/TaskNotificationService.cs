namespace CulinaryCommand.Services;

public interface ITaskNotificationService
{
    event Func<int, Task>? OnTasksChanged; // locationId
    Task NotifyTasksChangedAsync(int locationId);

    void NotifyTaskAssigned(CulinaryCommand.Data.Entities.Tasks assignedTask);
}

public class TaskNotificationService : ITaskNotificationService
{
    public event Func<int, Task>? OnTasksChanged;

    public async Task NotifyTasksChangedAsync(int locationId)
    {
        if (OnTasksChanged is not null)
            await OnTasksChanged.Invoke(locationId);
    }

    public void NotifyTaskAssigned(CulinaryCommand.Data.Entities.Tasks assignedTask)
    {
        // For now, treat assignment as a task-board refresh event.
        // (Keeps SmartTask story behavior without introducing a new per-user channel.)
        NotifyTasksChangedAsync(assignedTask.LocationId).GetAwaiter().GetResult();
    }
}