using System;

namespace TaskManagementApp.Models
{
    // INHERITANCE - TaskItem inherits from BaseEntity
    public class TaskItem : BaseEntity
    {
        // ENCAPSULATION - private fields with validation
        private string _priority = "Medium";
        private string _status = "To Do";

        public int ProjectID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? AssignedTo { get; set; }
        public int CreatedBy { get; set; }

        public string Priority
        {
            get { return _priority; }
            set
            {
                if (value != "Low" &&
                    value != "Medium" &&
                    value != "High")
                    throw new ArgumentException(
                        "Priority must be Low, Medium or High");
                _priority = value;
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                if (value != "To Do" &&
                    value != "In Progress" &&
                    value != "Done")
                    throw new ArgumentException(
                        "Status must be To Do, In Progress or Done");
                _status = value;
            }
        }

        public DateTime? DueDate { get; set; }

        // Navigation properties
        public Project? Project { get; set; }
        public User? User { get; set; }
        public ICollection<Comment> Comments { get; set; }
            = new List<Comment>();

        // POLYMORPHISM - overriding abstract methods
        public override string GetDisplayInfo()
        {
            return $"Task: {Title} | Priority: {_priority} " +
                   $"| Status: {_status}";
        }

        public override string GetEntityType()
        {
            return "TaskItem";
        }
    }
}