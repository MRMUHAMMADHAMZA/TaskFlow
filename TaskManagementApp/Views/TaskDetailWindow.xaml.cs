using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using TaskManagementApp.Data;
using TaskManagementApp.Helpers;
using TaskManagementApp.Models;

namespace TaskManagementApp.Views
{
    public partial class TaskDetailWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly int _taskId;

        public TaskDetailWindow(int taskId)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _taskId = taskId;
            LoadTaskDetail();
            LoadComments();
        }

        // ══════════════════════════════════════
        // LOAD TASK DETAILS
        // ══════════════════════════════════════
        private void LoadTaskDetail()
        {
            try
            {
                var task = _context.Tasks
                    .Include(t => t.Project)
                    .Include(t => t.User)
                    .FirstOrDefault(t => t.ID == _taskId);

                if (task == null) { Close(); return; }

                txtTaskTitle.Text = task.Title;
                txtProject.Text = task.Project?.ProjectName ?? "No Project";
                txtAssignedTo.Text = task.User?.FullName ?? "Unassigned";
                txtDueDate.Text = task.DueDate.HasValue
                                     ? task.DueDate.Value.ToString("MMM dd, yyyy")
                                     : "No due date";
                txtCreated.Text = task.CreatedDate.ToString("MMM dd, yyyy");
                txtDescription.Text =
                    string.IsNullOrWhiteSpace(task.Description)
                    ? "No description provided."
                    : task.Description;

                // Priority badge
                borderPriority.Background = new System.Windows.Media
                    .SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media
                    .ColorConverter.ConvertFromString(
                        task.Priority switch
                        {
                            "High" => "#3A1020",
                            "Medium" => "#3A2A00",
                            "Low" => "#0A2A1A",
                            _ => "#1A1A2E"
                        }));
                txtPriority.Foreground = new System.Windows.Media
                    .SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media
                    .ColorConverter.ConvertFromString(
                        task.Priority switch
                        {
                            "High" => "#FF6584",
                            "Medium" => "#FFD166",
                            "Low" => "#2CB67D",
                            _ => "#A7A9BE"
                        }));
                txtPriority.Text = task.Priority;

                // Status badge
                borderStatus.Background = new System.Windows.Media
                    .SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media
                    .ColorConverter.ConvertFromString(
                        task.Status switch
                        {
                            "Done" => "#0A2A1A",
                            "In Progress" => "#3A2A00",
                            "To Do" => "#1E1040",
                            _ => "#1A1A2E"
                        }));
                txtStatus.Foreground = new System.Windows.Media
                    .SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media
                    .ColorConverter.ConvertFromString(
                        task.Status switch
                        {
                            "Done" => "#2CB67D",
                            "In Progress" => "#FFD166",
                            "To Do" => "#6C63FF",
                            _ => "#A7A9BE"
                        }));
                txtStatus.Text = task.Status;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading task: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // LOAD COMMENTS
        // ══════════════════════════════════════
        private void LoadComments()
        {
            try
            {
                var raw = _context.Comments
                    .Include(c => c.User)
                    .Where(c => c.TaskID == _taskId)
                    .OrderByDescending(c => c.CreatedDate)
                    .ToList();

                var comments = raw.Select(c => new CommentDisplayItem
                {
                    CommentID = c.ID,
                    UserName = c.User?.FullName ?? "Unknown",
                    UserInitial = c.User?.FullName?.Length > 0
                                  ? c.User.FullName[0].ToString().ToUpper()
                                  : "?",
                    Content = c.Content,
                    TimeAgo = GetTimeAgo(c.CreatedDate)
                }).ToList();

                txtCommentCount.Text =
                    $"{comments.Count} comment{(comments.Count == 1 ? "" : "s")}";

                if (comments.Count == 0)
                {
                    lstComments.Visibility = Visibility.Collapsed;
                    emptyComments.Visibility = Visibility.Visible;
                }
                else
                {
                    lstComments.ItemsSource = comments;
                    lstComments.Visibility = Visibility.Visible;
                    emptyComments.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading comments: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // ADD COMMENT
        // ══════════════════════════════════════
        private void btnAddComment_Click(object sender, RoutedEventArgs e)
        {
            string content = txtNewComment.Text.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                txtNewComment.Focus();
                return;
            }

            if (SessionManager.CurrentUser == null) return;

            try
            {
                btnAddComment.IsEnabled = false;

                var comment = new Comment
                {
                    TaskID = _taskId,
                    UserID = SessionManager.CurrentUser.ID,
                    Content = content
                };

                _context.Comments.Add(comment);
                _context.SaveChanges();

                new ActivityLogger(_context).Log(
                    SessionManager.CurrentUser.ID,
                    $"Added comment on task ID {_taskId}");

                txtNewComment.Text = "";
                LoadComments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding comment: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnAddComment.IsEnabled = true;
            }
        }

        // ══════════════════════════════════════
        // DELETE COMMENT
        // ══════════════════════════════════════
        private void btnDeleteComment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            int commentId = (int)btn.Tag;

            var result = MessageBox.Show(
                "Delete this comment?",
                "Delete Comment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var comment = _context.Comments.Find(commentId);
                if (comment == null) return;

                _context.Comments.Remove(comment);
                _context.SaveChanges();

                LoadComments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting comment: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════
        // CLOSE
        // ══════════════════════════════════════
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
    }

    // ══════════════════════════════════════
    // DISPLAY MODEL
    // ══════════════════════════════════════
    public class CommentDisplayItem
    {
        public int CommentID { get; set; }
        public string UserName { get; set; } = "";
        public string UserInitial { get; set; } = "";
        public string Content { get; set; } = "";
        public string TimeAgo { get; set; } = "";
    }
}