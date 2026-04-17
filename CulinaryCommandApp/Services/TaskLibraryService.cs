using CulinaryCommand.Data;
using CulinaryCommand.Data.Entities;
using CulinaryCommand.Data.Models;
using Microsoft.EntityFrameworkCore;
using CulinaryCommand.Data.Enums;
using RecipeEntity = CulinaryCommandApp.Recipe.Entities.Recipe;

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
                .AsNoTracking()
                .Where(t => t.LocationId == locationId && t.IsActive)
                .OrderBy(t => t.Station)
                .ThenBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<TaskList>> GetTaskListsByLocationAsync(int locationId)
        {
            return await _db.TaskLists
                .AsNoTracking()
                .Where(l => l.LocationId == locationId && l.IsActive)
                .Include(l => l.Items.Where(i => i.TaskTemplate != null && i.TaskTemplate.IsActive))
                    .ThenInclude(i => i.TaskTemplate!)
                .OrderBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<List<TaskTemplate>> GetTemplatesForTaskListAsync(int taskListId)
        {
            var taskList = await _db.TaskLists
                .AsNoTracking()
                .Include(tl => tl.Items.OrderBy(i => i.SortOrder))
                    .ThenInclude(i => i.TaskTemplate)
                .FirstOrDefaultAsync(tl => tl.Id == taskListId && tl.IsActive);

            if (taskList == null)
                return new List<TaskTemplate>();

            return taskList.Items
                .Where(i => i.TaskTemplate != null && i.TaskTemplate.IsActive)
                .OrderBy(i => i.SortOrder)
                .Select(i => i.TaskTemplate!)
                .ToList();
        }

        public async Task<TaskTemplate> CreateTemplateAsync(CreateTaskTemplateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Station))
                throw new ArgumentException("Station is required.");

            if (request.LocationId <= 0)
                throw new ArgumentException("A valid location is required.");

            var normalizedName = await ResolveTemplateNameAsync(request);
            var normalizedStation = request.Station.Trim();
            var normalizedPriority = string.IsNullOrWhiteSpace(request.Priority)
                ? "Normal"
                : request.Priority.Trim();

            var duplicateExists = await _db.TaskTemplates.AnyAsync(t =>
                t.LocationId == request.LocationId &&
                t.IsActive &&
                t.Name == normalizedName &&
                t.Station == normalizedStation);

            if (duplicateExists)
                throw new InvalidOperationException("A task template with this name and station already exists.");

            var template = new TaskTemplate
            {
                Name = normalizedName,
                Station = normalizedStation,
                Kind = request.Kind,
                Priority = normalizedPriority,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                DefaultEstimatedMinutes = request.DefaultEstimatedMinutes,
                LocationId = request.LocationId,
                CreatedByUserId = request.CreatedByUserId,
                RecipeId = request.Kind == WorkTaskKind.PrepFromRecipe ? request.RecipeId : null,
                IngredientId = request.Kind == WorkTaskKind.PrepFromRecipe ? request.IngredientId : null,
                Par = request.Kind == WorkTaskKind.PrepFromRecipe ? request.Par : null,
                Count = request.Kind == WorkTaskKind.PrepFromRecipe ? request.Count : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TaskTemplates.Add(template);
            await _db.SaveChangesAsync();

            return template;
        }

        public async Task<TaskTemplate> UpdateTemplateAsync(UpdateTaskTemplateRequest request)
        {
            if (request.Id <= 0)
                throw new ArgumentException("A valid template id is required.");

            if (string.IsNullOrWhiteSpace(request.Station))
                throw new ArgumentException("Station is required.");

            var template = await _db.TaskTemplates.FirstOrDefaultAsync(t => t.Id == request.Id);

            if (template == null)
                throw new InvalidOperationException("Task template not found.");

            var normalizedName = await ResolveTemplateNameAsync(request);
            var normalizedStation = request.Station.Trim();
            var normalizedPriority = string.IsNullOrWhiteSpace(request.Priority)
                ? "Normal"
                : request.Priority.Trim();

            var duplicateExists = await _db.TaskTemplates.AnyAsync(t =>
                t.Id != request.Id &&
                t.LocationId == request.LocationId &&
                t.IsActive &&
                t.Name == normalizedName &&
                t.Station == normalizedStation);

            if (duplicateExists)
                throw new InvalidOperationException("A task template with this name and station already exists.");

            template.Name = normalizedName;
            template.Station = normalizedStation;
            template.Kind = request.Kind;
            template.Priority = normalizedPriority;
            template.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            template.DefaultEstimatedMinutes = request.DefaultEstimatedMinutes;
            template.LocationId = request.LocationId;
            template.RecipeId = request.Kind == WorkTaskKind.PrepFromRecipe ? request.RecipeId : null;
            template.IngredientId = request.Kind == WorkTaskKind.PrepFromRecipe ? request.IngredientId : null;
            template.Par = request.Kind == WorkTaskKind.PrepFromRecipe ? request.Par : null;
            template.Count = request.Kind == WorkTaskKind.PrepFromRecipe ? request.Count : null;
            template.IsActive = request.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return template;
        }

        public async Task ArchiveTemplateAsync(int templateId)
        {
            var template = await _db.TaskTemplates.FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null)
                throw new InvalidOperationException("Template not found.");

            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task<TaskList> CreateTaskListAsync(CreateTaskListRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Task list name is required.");

            if (request.LocationId <= 0)
                throw new ArgumentException("A valid location is required.");

            var normalizedName = request.Name.Trim();

            var duplicateExists = await _db.TaskLists.AnyAsync(l =>
                l.LocationId == request.LocationId &&
                l.IsActive &&
                l.Name == normalizedName);

            if (duplicateExists)
                throw new InvalidOperationException("A task list with this name already exists.");

            var taskList = new TaskList
            {
                Name = normalizedName,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                LocationId = request.LocationId,
                CreatedByUserId = request.CreatedByUserId,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TaskLists.Add(taskList);
            await _db.SaveChangesAsync();

            var validTemplateIds = await _db.TaskTemplates
                .Where(t =>
                    request.TaskTemplateIds.Contains(t.Id) &&
                    t.LocationId == request.LocationId &&
                    t.IsActive)
                .OrderBy(t => t.Station)
                .ThenBy(t => t.Name)
                .Select(t => t.Id)
                .ToListAsync();

            if (validTemplateIds.Any())
            {
                var sortOrder = 0;
                foreach (var templateId in validTemplateIds.Distinct())
                {
                    _db.TaskListItems.Add(new TaskListItem
                    {
                        TaskListId = taskList.Id,
                        TaskTemplateId = templateId,
                        SortOrder = sortOrder++
                    });
                }

                taskList.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return taskList;
        }

        public async Task<TaskList> UpdateTaskListAsync(UpdateTaskListRequest request)
        {
            if (request.Id <= 0)
                throw new ArgumentException("A valid task list id is required.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Task list name is required.");

            var taskList = await _db.TaskLists.FirstOrDefaultAsync(l => l.Id == request.Id);

            if (taskList == null)
                throw new InvalidOperationException("Task list not found.");

            var normalizedName = request.Name.Trim();

            var duplicateExists = await _db.TaskLists.AnyAsync(l =>
                l.Id != request.Id &&
                l.LocationId == request.LocationId &&
                l.IsActive &&
                l.Name == normalizedName);

            if (duplicateExists)
                throw new InvalidOperationException("A task list with this name already exists.");

            taskList.Name = normalizedName;
            taskList.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            taskList.LocationId = request.LocationId;
            taskList.IsActive = request.IsActive;
            taskList.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return taskList;
        }

        public async Task ArchiveTaskListAsync(int taskListId)
        {
            var taskList = await _db.TaskLists.FirstOrDefaultAsync(l => l.Id == taskListId);

            if (taskList == null)
                throw new InvalidOperationException("Task list not found.");

            taskList.IsActive = false;
            taskList.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task AddTemplatesToTaskListAsync(int taskListId, List<int> taskTemplateIds)
        {
            if (taskTemplateIds == null || !taskTemplateIds.Any())
                return;

            var taskList = await _db.TaskLists
                .Include(l => l.Items)
                .FirstOrDefaultAsync(l => l.Id == taskListId && l.IsActive);

            if (taskList == null)
                throw new InvalidOperationException("Task list not found.");

            var validTemplateIds = await _db.TaskTemplates
                .Where(t => taskTemplateIds.Contains(t.Id) && t.IsActive)
                .Select(t => t.Id)
                .ToListAsync();

            var existingTemplateIds = taskList.Items
                .Select(i => i.TaskTemplateId)
                .ToHashSet();

            var nextSortOrder = taskList.Items.Any()
                ? taskList.Items.Max(i => i.SortOrder) + 1
                : 0;

            foreach (var templateId in validTemplateIds.Distinct())
            {
                if (existingTemplateIds.Contains(templateId))
                    continue;

                taskList.Items.Add(new TaskListItem
                {
                    TaskListId = taskListId,
                    TaskTemplateId = templateId,
                    SortOrder = nextSortOrder++
                });
            }

            taskList.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task RemoveTemplateFromTaskListAsync(int taskListId, int taskTemplateId)
        {
            var item = await _db.TaskListItems
                .FirstOrDefaultAsync(i => i.TaskListId == taskListId && i.TaskTemplateId == taskTemplateId);

            if (item == null)
                return;

            _db.TaskListItems.Remove(item);

            var taskList = await _db.TaskLists.FirstOrDefaultAsync(l => l.Id == taskListId);
            if (taskList != null)
            {
                taskList.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
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

            var createdTasks = templates
                .Select(template => BuildTaskFromTemplate(template, userId, dueDate, assigner, priorityOverride))
                .ToList();

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

            var createdTasks = taskList.Items
                .OrderBy(i => i.SortOrder)
                .Where(i => i.TaskTemplate != null && i.TaskTemplate.IsActive)
                .Select(i => BuildTaskFromTemplate(i.TaskTemplate!, userId, dueDate, assigner, priorityOverride))
                .ToList();

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
                Status = Data.Enums.TaskStatus.Pending,
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

        private async Task<string> ResolveTemplateNameAsync(CreateTaskTemplateRequest request)
        {
            if (request.Kind != WorkTaskKind.PrepFromRecipe)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    throw new ArgumentException("Template name is required.");

                return request.Name.Trim();
            }

            var recipe = await GetRecipeForTemplateAsync(request.LocationId, request.RecipeId);
            return recipe.Title.Trim();
        }

        private async Task<string> ResolveTemplateNameAsync(UpdateTaskTemplateRequest request)
        {
            if (request.Kind != WorkTaskKind.PrepFromRecipe)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    throw new ArgumentException("Template name is required.");

                return request.Name.Trim();
            }

            var recipe = await GetRecipeForTemplateAsync(request.LocationId, request.RecipeId);
            return recipe.Title.Trim();
        }

        private async Task<RecipeEntity> GetRecipeForTemplateAsync(int locationId, int? recipeId)
        {
            if (!recipeId.HasValue)
                throw new ArgumentException("A recipe is required for prep-from-recipe templates.");

            var recipe = await _db.Recipes
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RecipeId == recipeId.Value && r.LocationId == locationId);

            if (recipe == null)
                throw new InvalidOperationException("The selected recipe could not be found for this location.");

            return recipe;
        }
    }
}
