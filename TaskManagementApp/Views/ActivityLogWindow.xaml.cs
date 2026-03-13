using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using TaskManagementApp.Data;
using TaskManagementApp.Helpers;

namespace TaskManagementApp.Views
{
    public partial class ActivityLogWindow : Window
    {
        private readonly AppDbContext _context;
        private List<LogDisplayItem> _allLogs = new();
        private bool _isLoaded = false;

        public ActivityLogWindow()
        {
            InitializeComponent();

            // ── Block non-admins ──
            if (!SessionManager.IsAdmin)
            {
                MessageBox.Show(
                    "Access denied. Only admins can view the Activity Log.",
                    "Access Denied", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Close();
                return;
            }

            _context = new AppDbContext();
            LoadSidebarInfo();
            LoadUserFilter();
            LoadLogs();
            _isLoaded = true;
        }

        // ══════════════════════════════════════
        // SIDEBAR
        // ══════════════════════════════════════
        private void LoadSidebarInfo()
        {
            var user = SessionManager.CurrentUser;
            if (user == null) return;

            txtSidebarName.Text = user.FullName;
            txtSidebarRole.Text = user.Role;
            txtAvatarInitial.Text = user.FullName.Length > 0
                ? user.FullName[0].ToString().ToUpper()
                : "?";
        }

        // ══════════════════════════════════════
        // LOAD USER FILTER DROPDOWN
        // ══════════════════════════════════════
        private void LoadUserFilter()
        {
            var users = _context.Users
                .OrderBy(u => u.FullName)
                .ToList();

            cmbFilterUser.Items.Clear();
            cmbFilterUser.Items.Add(
                new ComboBoxItem
                {
                    Content = "All Users",
                    IsSelected = true
                });

            foreach (var u in users)
                cmbFilterUser.Items.Add(
                    new ComboBoxItem
                    {
                        Content = u.FullName,
                        Tag = u.ID
                    });

            cmbFilterUser.SelectedIndex = 0;
        }

        // ══════════════════════════════════════
        // LOAD LOGS
        // ══════════════════════════════════════
        private void LoadLogs()
        {
            try
            {
                var raw = _context.ActivityLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.CreatedDate)
                    .ToList();

                int counter = 1;
                _allLogs = raw.Select(a => new LogDisplayItem
                {
                    RowNumber = counter++,
                    UserName = a.User?.FullName ?? "System",
                    UserInitial = a.User?.FullName?.Length > 0
                                  ? a.User.FullName[0].ToString().ToUpper()
                                  : "S",
                    Action = a.Action,
                    ActionColor = GetActionColor(a.Action),
                    TimeAgo = GetTimeAgo(a.CreatedDate),
                    FullDate = a.CreatedDate
                                   .ToString("MMM dd, yyyy  hh:mm tt"),
                    CreatedDate = a.CreatedDate,
                    UserId = a.UserID
                }).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading logs: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // APPLY FILTERS
        // ══════════════════════════════════════
        private void ApplyFilters()
        {
            var filtered = _allLogs.AsEnumerable();

            string userName = (cmbFilterUser.SelectedItem
                as ComboBoxItem)?.Content?.ToString() ?? "All Users";
            if (userName != "All Users")
                filtered = filtered.Where(l => l.UserName == userName);

            string dateRange = (cmbFilterDate.SelectedItem
                as ComboBoxItem)?.Content?.ToString() ?? "All Time";

            filtered = dateRange switch
            {
                "Today" => filtered.Where(
                    l => l.CreatedDate.Date == DateTime.Today),
                "Last 7 Days" => filtered.Where(
                    l => l.CreatedDate >= DateTime.Today.AddDays(-7)),
                "Last 30 Days" => filtered.Where(
                    l => l.CreatedDate >= DateTime.Today.AddDays(-30)),
                _ => filtered
            };

            var result = filtered.ToList();

            txtLogCount.Text =
                $"{result.Count} log{(result.Count == 1 ? "" : "s")}";

            if (result.Count == 0)
            {
                lstLogs.Visibility = Visibility.Collapsed;
                tableHeader.Visibility = Visibility.Collapsed;
                emptyState.Visibility = Visibility.Visible;
            }
            else
            {
                lstLogs.ItemsSource = result;
                lstLogs.Visibility = Visibility.Visible;
                tableHeader.Visibility = Visibility.Visible;
                emptyState.Visibility = Visibility.Collapsed;
            }
        }

        // ══════════════════════════════════════
        // FILTER EVENT
        // ══════════════════════════════════════
        private void Filter_Changed(object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoaded) ApplyFilters();
        }

        // ══════════════════════════════════════
        // REFRESH
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

            _isLoaded = false;
            LoadUserFilter();
            LoadLogs();
            _isLoaded = true;
        }

        // ══════════════════════════════════════
        // CLEAR ALL LOGS (Admin only)
        // ══════════════════════════════════════
        private void btnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.IsAdmin)
            {
                MessageBox.Show(
                    "Only admins can clear logs.",
                    "Access Denied", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Are you sure you want to clear ALL activity logs?\n" +
                "This cannot be undone.",
                "Clear Logs",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _context.ActivityLogs.RemoveRange(
                    _context.ActivityLogs.ToList());
                _context.SaveChanges();

                LoadLogs();

                MessageBox.Show(
                    "All logs cleared successfully.",
                    "Cleared", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error clearing logs: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // ACTION COLOR HELPER
        // ══════════════════════════════════════
        private string GetActionColor(string action)
        {
            string lower = action.ToLower();

            if (lower.Contains("login") ||
                lower.Contains("registered")) return "#2CB67D";
            if (lower.Contains("created")) return "#6C63FF";
            if (lower.Contains("updated") ||
                lower.Contains("edited")) return "#FFD166";
            if (lower.Contains("deleted")) return "#FF6584";
            if (lower.Contains("logout")) return "#A7A9BE";

            return "#6C63FF";
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
        // SIDEBAR NAVIGATION
        // ══════════════════════════════════════
        private void btnNavDashboard_Click(object sender,
            RoutedEventArgs e)
        {
            try { new DashboardWindow().Show(); Close(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNavProjects_Click(object sender,
            RoutedEventArgs e)
        {
            try { new ProjectsWindow().Show(); Close(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNavTasks_Click(object sender,
            RoutedEventArgs e)
        {
            try { new TasksWindow().Show(); Close(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // LOGOUT
        // ══════════════════════════════════════
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Logout", MessageBoxButton.YesNo,
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
    // DISPLAY MODEL
    // ══════════════════════════════════════
    public class LogDisplayItem
    {
        public int RowNumber { get; set; }
        public string UserName { get; set; } = "";
        public string UserInitial { get; set; } = "";
        public string Action { get; set; } = "";
        public string ActionColor { get; set; } = "";
        public string TimeAgo { get; set; } = "";
        public string FullDate { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public int UserId { get; set; }
    }
}