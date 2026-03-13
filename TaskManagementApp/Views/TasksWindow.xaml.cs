using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using TaskManagementApp.Data;
using TaskManagementApp.Helpers;
using TaskManagementApp.Models;

namespace TaskManagementApp.Views
{
    public partial class TasksWindow : Window
    {
        private readonly AppDbContext _context;
        private int? _editingTaskId = null;
        private List<TaskDisplayItem> _allTasks = new();
        private bool _isLoaded = false;

        public TasksWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadSidebarInfo();
            LoadFilterProjects();
            LoadTasks();
            HideForm();
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

            // Hide Activity Log for non-admins
            if (!SessionManager.IsAdmin)
                btnNavActivity.Visibility = Visibility.Collapsed;
        }

        // ══════════════════════════════════════
        // LOAD FILTER DROPDOWNS
        // ══════════════════════════════════════
        private void LoadFilterProjects()
        {
            var projects = _context.Projects
                .OrderBy(p => p.ProjectName)
                .ToList();

            cmbFilterProject.Items.Clear();
            cmbFilterProject.Items.Add(
                new ComboBoxItem
                {
                    Content = "All Projects",
                    IsSelected = true
                });

            foreach (var p in projects)
                cmbFilterProject.Items.Add(
                    new ComboBoxItem
                    {
                        Content = p.ProjectName,
                        Tag = p.ID
                    });

            cmbFilterProject.SelectedIndex = 0;
        }

        // ══════════════════════════════════════
        // LOAD TASKS
        // ══════════════════════════════════════
        private void LoadTasks()
        {
            try
            {
                var raw = _context.Tasks
                    .Include(t => t.Project)
                    .Include(t => t.User)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                _allTasks = raw.Select(t => new TaskDisplayItem
                {
                    TaskID = t.ID,
                    Title = t.Title,
                    ProjectName = t.Project?.ProjectName ?? "No Project",
                    ProjectID = t.ProjectID,
                    AssignedToName = t.User?.FullName ?? "Unassigned",
                    AssignedToId = t.AssignedTo,
                    CreatedBy = t.CreatedBy,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate.HasValue
                                     ? t.DueDate.Value.ToString("MMM dd, yyyy")
                                     : "No due date",
                    DueDateColor = GetDueDateColor(t.DueDate),
                    PriorityColor = t.Priority switch
                    {
                        "High" => "#FF6584",
                        "Medium" => "#FFD166",
                        "Low" => "#2CB67D",
                        _ => "#A7A9BE"
                    },
                    StatusColor = t.Status switch
                    {
                        "Done" => "#2CB67D",
                        "In Progress" => "#FFD166",
                        "To Do" => "#6C63FF",
                        _ => "#A7A9BE"
                    }
                }).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading tasks: {ex.Message}\n" +
                    $"{ex.InnerException?.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // APPLY FILTERS
        // ══════════════════════════════════════
        private void ApplyFilters()
        {
            var filtered = _allTasks.AsEnumerable();

            string status = (cmbFilterStatus.SelectedItem
                as ComboBoxItem)?.Content?.ToString() ?? "All Status";
            if (status != "All Status")
                filtered = filtered.Where(t => t.Status == status);

            string priority = (cmbFilterPriority.SelectedItem
                as ComboBoxItem)?.Content?.ToString() ?? "All Priority";
            if (priority != "All Priority")
                filtered = filtered.Where(t => t.Priority == priority);

            string project = (cmbFilterProject.SelectedItem
                as ComboBoxItem)?.Content?.ToString() ?? "All Projects";
            if (project != "All Projects")
                filtered = filtered.Where(t => t.ProjectName == project);

            var result = filtered.ToList();

            txtTaskCount.Text =
                $"{result.Count} task{(result.Count == 1 ? "" : "s")}";

            if (result.Count == 0)
            {
                lstTasks.Visibility = Visibility.Collapsed;
                tableHeader.Visibility = Visibility.Collapsed;
                emptyState.Visibility = Visibility.Visible;
            }
            else
            {
                lstTasks.ItemsSource = result;
                lstTasks.Visibility = Visibility.Visible;
                tableHeader.Visibility = Visibility.Visible;
                emptyState.Visibility = Visibility.Collapsed;
            }
        }

        // ══════════════════════════════════════
        // FILTER EVENTS
        // ══════════════════════════════════════
        private void cmbFilterStatus_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoaded) ApplyFilters();
        }

        private void cmbFilterPriority_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoaded) ApplyFilters();
        }

        private void cmbFilterProject_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoaded) ApplyFilters();
        }

        // ══════════════════════════════════════
        // SHOW / HIDE FORM
        // ══════════════════════════════════════
        private void ShowForm()
        {
            colForm.Width = new GridLength(340);
            borderError.Visibility = Visibility.Collapsed;
            LoadFormDropdowns();
        }

        private void HideForm()
        {
            colForm.Width = new GridLength(0);
            _editingTaskId = null;
            txtTitle.Text = "";
            txtDescription.Text = "";
            dpDueDate.SelectedDate = null;
            txtFormTitle.Text = "New Task";
            btnSave.Content = "Save Task";
            errTitle.Visibility = Visibility.Collapsed;
            errProject.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;
        }

        // ══════════════════════════════════════
        // LOAD FORM DROPDOWNS
        // ══════════════════════════════════════
        private void LoadFormDropdowns()
        {
            var projects = _context.Projects
                .OrderBy(p => p.ProjectName).ToList();

            cmbProject.Items.Clear();
            cmbProject.Items.Add(
                new ComboBoxItem { Content = "Select a project", Tag = 0 });
            foreach (var p in projects)
                cmbProject.Items.Add(
                    new ComboBoxItem { Content = p.ProjectName, Tag = p.ID });
            cmbProject.SelectedIndex = 0;

            var users = _context.Users
                .OrderBy(u => u.FullName).ToList();

            cmbAssignTo.Items.Clear();
            cmbAssignTo.Items.Add(
                new ComboBoxItem { Content = "Unassigned", Tag = 0 });
            foreach (var u in users)
                cmbAssignTo.Items.Add(
                    new ComboBoxItem { Content = u.FullName, Tag = u.ID });
            cmbAssignTo.SelectedIndex = 0;
        }

        // ══════════════════════════════════════
        // NEW TASK BUTTON
        // ══════════════════════════════════════
        private void btnNewTask_Click(object sender, RoutedEventArgs e)
        {
            _editingTaskId = null;
            txtFormTitle.Text = "New Task";
            txtTitle.Text = "";
            txtDescription.Text = "";
            dpDueDate.SelectedDate = null;
            btnSave.Content = "Save Task";
            cmbPriority.SelectedIndex = 1;
            cmbStatus.SelectedIndex = 0;
            ShowForm();
            txtTitle.Focus();
        }

        // ══════════════════════════════════════
        // VIEW BUTTON
        // ══════════════════════════════════════
        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            int taskId = (int)btn.Tag;

            try
            {
                var detail = new TaskDetailWindow(taskId);
                detail.Owner = this;
                detail.ShowDialog();
                LoadTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening task detail: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // EDIT BUTTON
        // ══════════════════════════════════════
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            int taskId = (int)btn.Tag;

            var task = _context.Tasks.Find(taskId);
            if (task == null) return;

            // Non-admins can only edit their own tasks
            if (!SessionManager.IsAdmin)
            {
                int currentUserId = SessionManager.CurrentUser?.ID ?? 0;
                if (task.AssignedTo != currentUserId &&
                    task.CreatedBy != currentUserId)
                {
                    MessageBox.Show(
                        "You can only edit tasks assigned to or created by you.",
                        "Access Denied", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            _editingTaskId = taskId;
            txtFormTitle.Text = "Edit Task";
            txtTitle.Text = task.Title;
            txtDescription.Text = task.Description ?? "";
            dpDueDate.SelectedDate = task.DueDate;
            btnSave.Content = "Update Task";
            ShowForm();

            foreach (ComboBoxItem item in cmbProject.Items)
                if (item.Tag is int id && id == task.ProjectID)
                { cmbProject.SelectedItem = item; break; }

            foreach (ComboBoxItem item in cmbAssignTo.Items)
                if (item.Tag is int uid && uid == task.AssignedTo)
                { cmbAssignTo.SelectedItem = item; break; }

            foreach (ComboBoxItem item in cmbPriority.Items)
                if (item.Content?.ToString() == task.Priority)
                { cmbPriority.SelectedItem = item; break; }

            foreach (ComboBoxItem item in cmbStatus.Items)
                if (item.Content?.ToString() == task.Status)
                { cmbStatus.SelectedItem = item; break; }

            txtTitle.Focus();
        }

        // ══════════════════════════════════════
        // DELETE BUTTON
        // ══════════════════════════════════════
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            int taskId = (int)btn.Tag;

            var task = _context.Tasks.Find(taskId);
            if (task == null) return;

            // Non-admins can only delete their own tasks
            if (!SessionManager.IsAdmin)
            {
                int currentUserId = SessionManager.CurrentUser?.ID ?? 0;
                if (task.AssignedTo != currentUserId &&
                    task.CreatedBy != currentUserId)
                {
                    MessageBox.Show(
                        "You can only delete tasks assigned to or created by you.",
                        "Access Denied", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show(
                $"Delete task '{task.Title}'?",
                "Delete Task",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _context.Tasks.Remove(task);
                _context.SaveChanges();

                if (SessionManager.CurrentUser != null)
                    new ActivityLogger(_context).Log(
                        SessionManager.CurrentUser.ID,
                        $"Deleted task: {task.Title}");

                HideForm();
                LoadTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error deleting task: {ex.Message}\n" +
                    $"{ex.InnerException?.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // SAVE BUTTON
        // ══════════════════════════════════════
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            errTitle.Visibility = Visibility.Collapsed;
            errProject.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;

            string title = txtTitle.Text.Trim();
            string description = txtDescription.Text.Trim();
            bool hasErrors = false;

            if (string.IsNullOrWhiteSpace(title))
            {
                errTitle.Text = "Task title is required";
                errTitle.Visibility = Visibility.Visible;
                hasErrors = true;
            }
            else if (title.Length < 3)
            {
                errTitle.Text = "Title must be at least 3 characters";
                errTitle.Visibility = Visibility.Visible;
                hasErrors = true;
            }

            int projectId = 0;
            if (cmbProject.SelectedItem is ComboBoxItem proj &&
                proj.Tag is int pid)
                projectId = pid;

            if (projectId == 0)
            {
                errProject.Text = "Please select a project";
                errProject.Visibility = Visibility.Visible;
                hasErrors = true;
            }

            if (hasErrors) return;

            int? assignedTo = null;
            if (cmbAssignTo.SelectedItem is ComboBoxItem usr &&
                usr.Tag is int uid && uid != 0)
                assignedTo = uid;

            string priority = (cmbPriority.SelectedItem
                as ComboBoxItem)?.Content?.ToString() ?? "Medium";
            string status = (cmbStatus.SelectedItem
                as ComboBoxItem)?.Content?.ToString() ?? "To Do";

            try
            {
                btnSave.IsEnabled = false;

                if (_editingTaskId == null)
                {
                    // ── CREATE ──
                    var task = new TaskItem
                    {
                        Title = title,
                        Description = description,
                        ProjectID = projectId,
                        AssignedTo = assignedTo,
                        Priority = priority,
                        Status = status,
                        DueDate = dpDueDate.SelectedDate,
                        CreatedBy = SessionManager.CurrentUser?.ID ?? 0
                    };

                    _context.Tasks.Add(task);
                    _context.SaveChanges();

                    if (SessionManager.CurrentUser != null)
                        new ActivityLogger(_context).Log(
                            SessionManager.CurrentUser.ID,
                            $"Created task: {title}");
                }
                else
                {
                    // ── UPDATE ──
                    var task = _context.Tasks.Find(_editingTaskId.Value);
                    if (task != null)
                    {
                        task.Title = title;
                        task.Description = description;
                        task.ProjectID = projectId;
                        task.AssignedTo = assignedTo;
                        task.Priority = priority;
                        task.Status = status;
                        task.DueDate = dpDueDate.SelectedDate;
                        _context.SaveChanges();

                        if (SessionManager.CurrentUser != null)
                            new ActivityLogger(_context).Log(
                                SessionManager.CurrentUser.ID,
                                $"Updated task: {title}");
                    }
                }

                HideForm();
                LoadTasks();
            }
            catch (Exception ex)
            {
                txtError.Text =
                    $"Error: {ex.Message}\n{ex.InnerException?.Message}";
                borderError.Visibility = Visibility.Visible;
            }
            finally
            {
                btnSave.IsEnabled = true;
            }
        }

        // ══════════════════════════════════════
        // CANCEL
        // ══════════════════════════════════════
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            HideForm();
        }

        // ══════════════════════════════════════
        // DUE DATE COLOR HELPER
        // ══════════════════════════════════════
        private string GetDueDateColor(DateTime? dueDate)
        {
            if (!dueDate.HasValue) return "#A7A9BE";
            if (dueDate.Value.Date < DateTime.Today) return "#FF6584";
            if (dueDate.Value.Date == DateTime.Today) return "#FFD166";
            return "#A7A9BE";
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

        private void btnNavActivity_Click(object sender,
            RoutedEventArgs e)
        {
            try { new ActivityLogWindow().Show(); Close(); }
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
    public class TaskDisplayItem
    {
        public int TaskID { get; set; }
        public string Title { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public int ProjectID { get; set; }
        public string AssignedToName { get; set; } = "";
        public int? AssignedToId { get; set; }
        public int CreatedBy { get; set; }
        public string Priority { get; set; } = "";
        public string PriorityColor { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = "";
        public string DueDate { get; set; } = "";
        public string DueDateColor { get; set; } = "";
    }
}