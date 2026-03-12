using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using CulinaryCommand.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace CulinaryCommand.Services
{
    public class TaskLibraryService : ITaskLibraryService
    {
        private readonly AppDbContext _db;

        public TaskLibraryService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<TaskTemplate>> GetTemplatesByLocationAsync(int locationId)
        {
            return await _db.TaskTemplates
                .Where(t => t.LocationId == locationId && t.IsActive)
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<TaskList>> GetTaskListsByLocationAsync(int locationId)
        {
            return await _db.TaskLists
                .Where(tl => tl.LocationId == locationId && tl.IsActive)
                .Include(tl => tl.Items)
                    .ThenInclude(i => i.TaskTemplate)
                .OrderBy(tl => tl.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<TaskTemplate>> GetTemplatesForTaskListAsync(int taskListId)
        {
            var taskList = await _db.TaskLists
                .Include(tl => tl.Items.OrderBy(i => i.SortOrder))
                    .ThenInclude(i => i.TaskTemplate)
                .AsNoTracking()
                .FirstOrDefaultAsync(tl => tl.Id == taskListId);

            if (taskList == null)
                return new List<TaskTemplate>();

            return taskList.Items
                .Where(i => i.TaskTemplate != null && i.TaskTemplate.IsActive)
                .OrderBy(i => i.SortOrder)
                .Select(i => i.TaskTemplate!)
                .ToList();
        }

        public async Task<List<Tasks>> AssignTemplatesAsync(
            List<int> taskTemplateIds,
            int? userId,
            DateTime dueDate,
            string assigner,
            string? priorityOverride = null)
        {
            if (taskTemplateIds == null || !taskTemplateIds.Any())
                return new List<Tasks>();

            var templates = await _db.TaskTemplates
                .Where(t => taskTemplateIds.Contains(t.Id) && t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var createdTasks = new List<Tasks>();

            foreach (var template in templates)
            {
                var task = BuildTaskFromTemplate(template, userId, dueDate, assigner, priorityOverride);
                createdTasks.Add(task);
            }

            _db.Tasks.AddRange(createdTasks);
            await _db.SaveChangesAsync();

            return createdTasks;
        }

        public async Task<List<Tasks>> AssignTaskListAsync(
            int taskListId,
            int? userId,
            DateTime dueDate,
            string assigner,
            string? priorityOverride = null)
        {
            var taskList = await _db.TaskLists
                .Include(tl => tl.Items.OrderBy(i => i.SortOrder))
                    .ThenInclude(i => i.TaskTemplate)
                .FirstOrDefaultAsync(tl => tl.Id == taskListId && tl.IsActive);

            if (taskList == null)
                return new List<Tasks>();

            var createdTasks = new List<Tasks>();

            foreach (var item in taskList.Items.OrderBy(i => i.SortOrder))
            {
                if (item.TaskTemplate == null || !item.TaskTemplate.IsActive)
                    continue;

                var task = BuildTaskFromTemplate(item.TaskTemplate, userId, dueDate, assigner, priorityOverride);
                createdTasks.Add(task);
            }

            _db.Tasks.AddRange(createdTasks);
            await _db.SaveChangesAsync();

            return createdTasks;
        }

        private static Tasks BuildTaskFromTemplate(
            TaskTemplate template,
            int? userId,
            DateTime dueDate,
            string assigner,
            string? priorityOverride = null)
        {
            var finalPriority = !string.IsNullOrWhiteSpace(priorityOverride)
                ? priorityOverride
                : template.Priority;

            return new Tasks
            {
                Name = template.Name,
                Station = template.Station,
                Status = CulinaryCommand.Data.Enums.TaskStatus.Pending,
                Assigner = assigner,
                Date = DateTime.UtcNow,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = dueDate,
                LocationId = template.LocationId,
                Kind = template.Kind,
                RecipeId = template.RecipeId,
                IngredientId = template.IngredientId,
                Par = template.Par,
                Count = template.Count,
                Priority = finalPriority ?? "Normal",
                Notes = template.Notes
            };
        }
    }
}