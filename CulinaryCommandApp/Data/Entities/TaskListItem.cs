using System.ComponentModel.DataAnnotations;

namespace CulinaryCommand.Data.Entities
{
    public class TaskListItem
    {
        [Key]
        public int Id { get; set; }

        public int TaskListId { get; set; }
        public TaskList? TaskList { get; set; }

        public int TaskTemplateId { get; set; }
        public TaskTemplate? TaskTemplate { get; set; }

        public int SortOrder { get; set; } = 0;
    }
}