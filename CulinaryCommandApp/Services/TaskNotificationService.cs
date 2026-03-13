namespace CulinaryCommand.Services;

public interface ITaskNotificationService
{
    event Func<int, Task>? OnTasksChanged; // locationId
    Task NotifyTasksChangedAsync(int locationId);
}

public class TaskNotificationService : ITaskNotificationService
{
    public event Func<int, Task>? OnTasksChanged;

    public async Task NotifyTasksChangedAsync(int locationId)
    {
        if (OnTasksChanged is not null)
            await OnTasksChanged.Invoke(locationId);
    }
}