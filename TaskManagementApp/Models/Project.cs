using System;
using System.Collections.Generic;

namespace TaskManagementApp.Models
{
    // INHERITANCE - Project inherits from BaseEntity
    public class Project : BaseEntity
    {
        // ENCAPSULATION - private fields
        private string _projectName = string.Empty;

        public string ProjectName
        {
            get { return _projectName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Project name cannot be empty");
                _projectName = value;
            }
        }

        public string? Description { get; set; }
        public int CreatedBy { get; set; }

        // ── Navigation Properties ──
        public User? User { get; set; }
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        // POLYMORPHISM - overriding abstract method
        public override string GetDisplayInfo()
        {
            return $"Project: {_projectName} | Tasks: {Tasks.Count}";
        }

        public override string GetEntityType()
        {
            return "Project";
        }
    }
}