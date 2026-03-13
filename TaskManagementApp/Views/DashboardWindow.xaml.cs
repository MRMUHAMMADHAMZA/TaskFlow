using Microsoft.EntityFrameworkCore;
using System.Windows;
using TaskManagementApp.Data;
using TaskManagementApp.Helpers;

namespace TaskManagementApp.Views
{
    public partial class DashboardWindow : Window
    {
        private readonly AppDbContext _context;

        public DashboardWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadUserInfo();
            LoadDashboardData();
        }

        // ══════════════════════════════════════
        // LOAD USER INFO INTO SIDEBAR
        // ══════════════════════════════════════
        private void LoadUserInfo()
        {
            var user = SessionManager.CurrentUser;
            if (user == null) return;

            string hour = DateTime.Now.Hour switch
            {
                < 12 => "morning",
                < 17 => "afternoon",
                _ => "evening"
            };

            txtGreeting.Text =
                $"Good {hour}, {user.FullName.Split(' ')[0]} 👋";
            txtDate.Text =
                DateTime.Now.ToString("dddd, MMMM dd yyyy");
            txtSidebarName.Text = user.FullName;
            txtSidebarRole.Text = user.Role;
            txtAvatarInitial.Text =
                user.FullName.Length > 0
                    ? user.FullName[0].ToString().ToUpper()
                    : "?";

            // Hide Activity Log for non-admins
            if (!SessionManager.IsAdmin)
                btnNavActivity.Visibility = Visibility.Collapsed;
        }

        // ══════════════════════════════════════
        // LOAD ALL DASHBOARD DATA
        // ══════════════════════════════════════
        private void LoadDashboardData()
        {
            LoadStats();
            LoadRecentTasks();
            LoadRecentActivity();
        }

        // ══════════════════════════════════════
        // STATS CARDS
        // ══════════════════════════════════════
        private void LoadStats()
        {
            try
            {
                int totalProjects = _context.Projects.Count();
                int totalTasks = _context.Tasks.Count();
                int inProgress = _context.Tasks
                    .Count(t => t.Status == "In Progress");
                int completed = _context.Tasks
                    .Count(t => t.Status == "Done");

                txtTotalProjects.Text = totalProjects.ToString();
                txtTotalTasks.Text = totalTasks.ToString();
                txtInProgress.Text = inProgress.ToString();
                txtCompleted.Text = completed.ToString();
            }
            catch
            {
                txtTotalProjects.Text =
                txtTotalTasks.Text =
                txtInProgress.Text =
                txtCompleted.Text = "—";
            }
        }

        // ══════════════════════════════════════
        // RECENT TASKS
        // ══════════════════════════════════════
        private void LoadRecentTasks()
        {
            try
            {
                var tasks = _context.Tasks
                    .Include(t => t.Project)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(6)
                    .Select(t => new DashboardTaskItem
                    {
                        Title = t.Title,
                        ProjectName = t.Project != null
                                      ? t.Project.ProjectName
                                      : "No Project",
                        Status = t.Status
                    })
                    .ToList();

                foreach (var task in tasks)
                {
                    task.StatusColor = task.Status switch
                    {
                        "Done" => "#2CB67D",
                        "In Progress" => "#FFD166",
                        "To Do" => "#6C63FF",
                        _ => "#A7A9BE"
                    };
                }

                if (tasks.Count == 0)
                {
                    lstRecentTasks.Visibility = Visibility.Collapsed;
                    emptyTasks.Visibility = Visibility.Visible;
                }
                else
                {
                    lstRecentTasks.ItemsSource = tasks;
                    lstRecentTasks.Visibility = Visibility.Visible;
                    emptyTasks.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                lstRecentTasks.Visibility = Visibility.Collapsed;
                emptyTasks.Visibility = Visibility.Visible;
            }
        }

        // ══════════════════════════════════════
        // RECENT ACTIVITY
        // ══════════════════════════════════════
        private void LoadRecentActivity()
        {
            try
            {
                var rawLogs = _context.ActivityLogs
                    .OrderByDescending(a => a.CreatedDate)
                    .Take(8)
                    .Select(a => new
                    {
                        a.Action,
                        a.CreatedDate
                    })
                    .ToList();

                var logs = rawLogs.Select(a => new ActivityDisplayItem
                {
                    Action = a.Action,
                    TimeAgo = GetTimeAgo(a.CreatedDate)
                }).ToList();

                if (logs.Count == 0)
                {
                    lstActivity.Visibility = Visibility.Collapsed;
                    emptyActivity.Visibility = Visibility.Visible;
                }
                else
                {
                    lstActivity.ItemsSource = logs;
                    lstActivity.Visibility = Visibility.Visible;
                    emptyActivity.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                lstActivity.Visibility = Visibility.Collapsed;
                emptyActivity.Visibility = Visibility.Visible;
            }
        }

        // ══════════════════════════════════════
        // TIME AGO HELPER
        // ══════════════════════════════════════
        private string GetTimeAgo(DateTime date)
        {
            var diff = DateTime.Now - date;

            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";

            return date.ToString("MMM dd");
        }

        // ══════════════════════════════════════
        // REFRESH BUTTON
        // ══════════════════════════════════════
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            var spin = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(
                    TimeSpan.FromMilliseconds(600)),
                RepeatBehavior = new System.Windows.Media.Animation
                                     .RepeatBehavior(1)
            };

            rotateTransform.BeginAnimation(
                System.Windows.Media.RotateTransform.AngleProperty,
                spin);

            LoadDashboardData();
        }

        // ══════════════════════════════════════
        // NEW TASK BUTTON
        // ══════════════════════════════════════
        private void btnNewTask_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new TasksWindow();
                win.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening Tasks: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // SIDEBAR NAVIGATION
        // ══════════════════════════════════════
        private void btnNavDashboard_Click(object sender,
            RoutedEventArgs e)
        {
            // Already on dashboard
        }

        private void btnNavProjects_Click(object sender,
            RoutedEventArgs e)
        {
            try
            {
                new ProjectsWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening Projects: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btnNavTasks_Click(object sender,
            RoutedEventArgs e)
        {
            try
            {
                new TasksWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening Tasks: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btnNavActivity_Click(object sender,
            RoutedEventArgs e)
        {
            try
            {
                new ActivityLogWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening Activity Log: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // LOGOUT
        // ══════════════════════════════════════
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SessionManager.Logout();
                new LoginWindow().Show();
                Close();
            }
        }
    }

    // ══════════════════════════════════════
    // DISPLAY MODELS
    // ══════════════════════════════════════
    public class DashboardTaskItem
    {
        public string Title { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = "";
    }

    public class ActivityDisplayItem
    {
        public string Action { get; set; } = "";
        public string TimeAgo { get; set; } = "";
    }
}