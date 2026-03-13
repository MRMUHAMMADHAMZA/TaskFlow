using System;
using System.Collections.Generic;
using System.Text;
using TaskManagementApp.Data;
using TaskManagementApp.Models;

namespace TaskManagementApp.Helpers
{
    public class ActivityLogger
    {
        private readonly AppDbContext _context;

        public ActivityLogger(AppDbContext context)
        {
            _context = context;
        }

        public void Log(int userId, string action)
        {
            var log = new ActivityLog
            {
                UserID = userId,
                Action = action
            };
            _context.ActivityLogs.Add(log);
            _context.SaveChanges();
        }
    }
}
