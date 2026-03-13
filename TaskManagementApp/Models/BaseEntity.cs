using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagementApp.Models
{
    // ABSTRACTION - abstract class cannot be instantiated directly
    public abstract class BaseEntity
    {
        // ENCAPSULATION - private field with public property
        private DateTime _createdDate;

        public int ID { get; set; }

        public DateTime CreatedDate
        {
            get { return _createdDate; }
            set { _createdDate = value; }
        }

        public BaseEntity()
        {
            _createdDate = DateTime.Now;
        }

        // ABSTRACTION - abstract method that every model must implement
        public abstract string GetDisplayInfo();

        // POLYMORPHISM - virtual method that can be overridden
        public virtual string GetEntityType()
        {
            return "Base Entity";
        }
    }
}
