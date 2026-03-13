using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using TaskManagementApp.Data;
using TaskManagementApp.Helpers;
using TaskManagementApp.Models;

namespace TaskManagementApp.Views
{
    public partial class ProjectsWindow : Window
    {
        private readonly AppDbContext _context;
        private int? _editingProjectId = null;

        public ProjectsWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadSidebarInfo();
            LoadProjects();
            HideForm();
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

            // Hide New Project button for non-admins
            if (!SessionManager.IsAdmin)
                btnNewProject.Visibility = Visibility.Collapsed;

            // Hide Activity Log for non-admins
            if (!SessionManager.IsAdmin)
                btnNavActivity.Visibility = Visibility.Collapsed;
        }

        // ══════════════════════════════════════
        // LOAD PROJECTS
        // ══════════════════════════════════════
        private void LoadProjects()
        {
            try
            {
                var projects = _context.Projects
                    .Include(p => p.Tasks)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToList();

                var display = projects.Select(p =>
                    new ProjectDisplayItem
                    {
                        ProjectID = p.ID,
                        ProjectName = p.ProjectName,
                        Description = string.IsNullOrWhiteSpace(p.Description)
                                      ? "No description"
                                      : p.Description,
                        TaskCount = p.Tasks?.Count ?? 0,
                        CreatedDate = p.CreatedDate.ToString("MMM dd, yyyy")
                    }).ToList();

                txtProjectCount.Text =
                    $"{display.Count} " +
                    $"project{(display.Count == 1 ? "" : "s")}";

                if (display.Count == 0)
                {
                    lstProjects.Visibility = Visibility.Collapsed;
                    emptyState.Visibility = Visibility.Visible;
                }
                else
                {
                    lstProjects.ItemsSource = display;
                    lstProjects.Visibility = Visibility.Visible;
                    emptyState.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading projects: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // SHOW / HIDE FORM
        // ══════════════════════════════════════
        private void ShowForm()
        {
            colForm.Width = new GridLength(320);
            borderError.Visibility = Visibility.Collapsed;
            errProjectName.Visibility = Visibility.Collapsed;
        }

        private void HideForm()
        {
            colForm.Width = new GridLength(0);
            _editingProjectId = null;
            txtProjectName.Text = "";
            txtDescription.Text = "";
            txtFormTitle.Text = "New Project";
            btnSave.Content = "Save Project";
            errProjectName.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;
        }

        // ══════════════════════════════════════
        // NEW PROJECT BUTTON
        // ══════════════════════════════════════
        private void btnNewProject_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.IsAdmin)
            {
                MessageBox.Show(
                    "Only admins can create projects.",
                    "Access Denied", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _editingProjectId = null;
            txtFormTitle.Text = "New Project";
            txtProjectName.Text = "";
            txtDescription.Text = "";
            btnSave.Content = "Save Project";
            ShowForm();
            txtProjectName.Focus();
        }

        // ══════════════════════════════════════
        // EDIT BUTTON
        // ══════════════════════════════════════
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.IsAdmin)
            {
                MessageBox.Show(
                    "Only admins can edit projects.",
                    "Access Denied", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (sender is not Button btn) return;
            int projectId = (int)btn.Tag;

            var project = _context.Projects.Find(projectId);
            if (project == null) return;

            _editingProjectId = projectId;
            txtFormTitle.Text = "Edit Project";
            txtProjectName.Text = project.ProjectName;
            txtDescription.Text = project.Description ?? "";
            btnSave.Content = "Update Project";
            ShowForm();
            txtProjectName.Focus();
        }

        // ══════════════════════════════════════
        // DELETE BUTTON
        // ══════════════════════════════════════
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.IsAdmin)
            {
                MessageBox.Show(
                    "Only admins can delete projects.",
                    "Access Denied", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (sender is not Button btn) return;
            int projectId = (int)btn.Tag;

            var project = _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefault(p => p.ID == projectId);
            if (project == null) return;

            string warning = project.Tasks?.Count > 0
                ? $"'{project.ProjectName}' has " +
                  $"{project.Tasks.Count} task(s).\n" +
                  $"Deleting will also delete all tasks.\n\n" +
                  $"Are you sure?"
                : $"Delete project '{project.ProjectName}'?";

            var result = MessageBox.Show(
                warning,
                "Delete Project",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                _context.Projects.Remove(project);
                _context.SaveChanges();

                if (SessionManager.CurrentUser != null)
                    new ActivityLogger(_context).Log(
                        SessionManager.CurrentUser.ID,
                        $"Deleted project: {project.ProjectName}");

                HideForm();
                LoadProjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error deleting project: {ex.Message}",
                    "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // SAVE BUTTON
        // ══════════════════════════════════════
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.IsAdmin)
            {
                MessageBox.Show(
                    "Only admins can save projects.",
                    "Access Denied", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            errProjectName.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;

            string name = txtProjectName.Text.Trim();
            string description = txtDescription.Text.Trim();
            bool hasErrors = false;

            if (string.IsNullOrWhiteSpace(name))
            {
                errProjectName.Text = "Project name is required";
                errProjectName.Visibility = Visibility.Visible;
                hasErrors = true;
            }
            else if (name.Length < 3)
            {
                errProjectName.Text = "Name must be at least 3 characters";
                errProjectName.Visibility = Visibility.Visible;
                hasErrors = true;
            }
            else if (name.Length > 150)
            {
                errProjectName.Text = "Name must be under 150 characters";
                errProjectName.Visibility = Visibility.Visible;
                hasErrors = true;
            }

            if (description.Length > 500)
            {
                txtError.Text = "Description must be under 500 characters";
                borderError.Visibility = Visibility.Visible;
                hasErrors = true;
            }

            if (hasErrors) return;

            try
            {
                btnSave.IsEnabled = false;

                if (_editingProjectId == null)
                {
                    // ── CREATE ──
                    var project = new Project
                    {
                        ProjectName = name,
                        Description = description,
                        CreatedBy = SessionManager.CurrentUser!.ID
                    };

                    _context.Projects.Add(project);
                    _context.SaveChanges();

                    if (SessionManager.CurrentUser != null)
                        new ActivityLogger(_context).Log(
                            SessionManager.CurrentUser.ID,
                            $"Created project: {name}");
                }
                else
                {
                    // ── UPDATE ──
                    var project = _context.Projects
                        .Find(_editingProjectId.Value);
                    if (project != null)
                    {
                        project.ProjectName = name;
                        project.Description = description;
                        _context.SaveChanges();

                        if (SessionManager.CurrentUser != null)
                            new ActivityLogger(_context).Log(
                                SessionManager.CurrentUser.ID,
                                $"Updated project: {name}");
                    }
                }

                HideForm();
                LoadProjects();
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
    public class ProjectDisplayItem
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = "";
        public string Description { get; set; } = "";
        public int TaskCount { get; set; }
        public string CreatedDate { get; set; } = "";
    }
}