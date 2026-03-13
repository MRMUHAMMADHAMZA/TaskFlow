using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace TaskManagementApp.Models
{
    // INHERITANCE - User inherits from BaseEntity
    public class User : BaseEntity
    {
        // ENCAPSULATION - private fields
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _role = "User";

        // Public properties with validation
        public string FullName
        {
            get { return _fullName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Full name cannot be empty");
                _fullName = value;
            }
        }

        public string Email
        {
            get { return _email; }
            set
            {
                if (!value.Contains("@"))
                    throw new ArgumentException("Invalid email address");
                _email = value;
            }
        }

        public string PasswordHash { get; set; } = string.Empty;

        public string Role
        {
            get { return _role; }
            set
            {
                if (value != "Admin" && value != "User")
                    throw new ArgumentException("Role must be Admin or User");
                _role = value;
            }
        }

        // Navigation properties
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // POLYMORPHISM - overriding abstract method
        public override string GetDisplayInfo()
        {
            return $"User: {_fullName} | Email: {_email} | Role: {_role}";
        }

        // POLYMORPHISM - overriding virtual method
        public override string GetEntityType()
        {
            return "User";
        }
    }
}
