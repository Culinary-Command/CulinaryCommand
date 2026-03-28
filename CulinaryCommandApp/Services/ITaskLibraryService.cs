using CulinaryCommand.Data.Entities;
using CulinaryCommand.Data.Models;

namespace CulinaryCommand.Services
{
    public interface ITaskLibraryService
    {
        Task<List<TaskTemplate>> GetTemplatesByLocationAsync(int locationId);
        Task<List<TaskList>> GetTaskListsByLocationAsync(int locationId);
        Task<List<TaskTemplate>> GetTemplatesForTaskListAsync(int taskListId);
        Task<TaskTemplate> CreateTemplateAsync(CreateTaskTemplateRequest request);
        Task<TaskTemplate> UpdateTemplateAsync(UpdateTaskTemplateRequest request);

        Task ArchiveTemplateAsync(int templateId);
        Task<TaskList> CreateTaskListAsync(CreateTaskListRequest request);
        Task<TaskList> UpdateTaskListAsync(UpdateTaskListRequest request);
        Task ArchiveTaskListAsync(int taskListId);

        Task AddTemplatesToTaskListAsync(int taskListId, List<int> taskTemplateIds);
        Task RemoveTemplateFromTaskListAsync(int taskListId, int taskTemplateId);

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