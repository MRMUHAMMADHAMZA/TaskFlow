using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagementApp.Models
{
    // INHERITANCE - ActivityLog inherits from BaseEntity
    public class ActivityLog : BaseEntity
    {
        public int UserID { get; set; }
        public string Action { get; set; } = string.Empty;

        // Navigation property
        public User? User { get; set; }

        // POLYMORPHISM - overriding abstract method
        public override string GetDisplayInfo()
        {
            return $"Log: {Action} at {CreatedDate}";
        }

        public override string GetEntityType()
        {
            return "ActivityLog";
        }
    }
}
