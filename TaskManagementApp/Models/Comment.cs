using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagementApp.Models
{
    // INHERITANCE - Comment inherits from BaseEntity
    public class Comment : BaseEntity
    {
        // ENCAPSULATION - private field with validation
        private string _content = string.Empty;

        public int TaskID { get; set; }
        public int UserID { get; set; }

        public string Content
        {
            get { return _content; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Comment cannot be empty");
                _content = value;
            }
        }

        // Navigation properties
        public TaskItem? Task { get; set; }
        public User? User { get; set; }

        // POLYMORPHISM - overriding abstract method
        public override string GetDisplayInfo()
        {
            return $"Comment by UserID {UserID}: {_content}";
        }

        public override string GetEntityType()
        {
            return "Comment";
        }
    }
}