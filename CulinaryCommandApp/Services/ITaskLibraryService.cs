using CulinaryCommand.Data.Entities;

namespace CulinaryCommand.Services
{
    public interface ITaskLibraryService
    {
        Task<List<TaskTemplate>> GetTemplatesByLocationAsync(int locationId);
        Task<List<TaskList>> GetTaskListsByLocationAsync(int locationId);
        Task<List<TaskTemplate>> GetTemplatesForTaskListAsync(int taskListId);

        Task<List<Tasks>> AssignTemplatesAsync(
            List<int> taskTemplateIds,
            int? userId,
            DateTime dueDate,
            string assigner,
            string? priorityOverride = null
        );

        Task<List<Tasks>> AssignTaskListAsync(
            int taskListId,
            int? userId,
            DateTime dueDate,
            string assigner,
            string? priorityOverride = null
        );
    }
}