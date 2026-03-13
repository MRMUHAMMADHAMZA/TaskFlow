using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TaskManagementApp.Data;
using TaskManagementApp.Helpers;
using TaskManagementApp.Models;

namespace TaskManagementApp.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly AppDbContext _context;
        private bool _showPassword = false;
        private bool _showConfirm = false;

        public RegisterWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
        }

        // ══════════════════════════════════════
        // CLEAR ERRORS WHILE TYPING
        // ══════════════════════════════════════
        private void txtFullName_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            errFullName.Visibility = Visibility.Collapsed;
        }

        private void txtEmail_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            errEmail.Visibility = Visibility.Collapsed;
        }

        private void txtConfirm_PasswordChanged(object sender,
            RoutedEventArgs e)
        {
            errConfirmPassword.Visibility = Visibility.Collapsed;
        }

        // ══════════════════════════════════════
        // EYE TOGGLE — PASSWORD
        // ══════════════════════════════════════
        private void btnTogglePassword_Click(object sender,
            RoutedEventArgs e)
        {
            _showPassword = !_showPassword;
            if (_showPassword)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
                btnTogglePassword.Content = "🙈";
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                btnTogglePassword.Content = "👁";
            }
        }

        // ══════════════════════════════════════
        // EYE TOGGLE — CONFIRM PASSWORD
        // ══════════════════════════════════════
        private void btnToggleConfirm_Click(object sender,
            RoutedEventArgs e)
        {
            _showConfirm = !_showConfirm;
            if (_showConfirm)
            {
                txtConfirmVisible.Text = txtConfirmPassword.Password;
                txtConfirmVisible.Visibility = Visibility.Visible;
                txtConfirmPassword.Visibility = Visibility.Collapsed;
                btnToggleConfirm.Content = "🙈";
            }
            else
            {
                txtConfirmPassword.Password = txtConfirmVisible.Text;
                txtConfirmPassword.Visibility = Visibility.Visible;
                txtConfirmVisible.Visibility = Visibility.Collapsed;
                btnToggleConfirm.Content = "👁";
            }
        }

        // ══════════════════════════════════════
        // PASSWORD STRENGTH METER
        // ══════════════════════════════════════
        private void txtPassword_PasswordChanged(object sender,
            RoutedEventArgs e)
        {
            string pwd = txtPassword.Password;
            errPassword.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(pwd))
            {
                strengthBar.Visibility = Visibility.Collapsed;
                txtStrength.Text = "";
                return;
            }

            strengthBar.Visibility = Visibility.Visible;

            int score = 0;
            if (pwd.Length >= 8) score++;
            if (Regex.IsMatch(pwd, @"[A-Z]")) score++;
            if (Regex.IsMatch(pwd, @"[0-9]")) score++;
            if (Regex.IsMatch(pwd, @"[^a-zA-Z0-9]")) score++;

            UpdateStrengthBar(score);
        }

        private void UpdateStrengthBar(int score)
        {
            string gray = "#2A2A4A";
            string[] colors = { gray, gray, gray, gray };
            string label = "";

            switch (score)
            {
                case 1:
                    colors[0] = "#FF6584";
                    label = "Weak — add uppercase & numbers";
                    txtStrength.Foreground = Brush("#FF6584");
                    break;
                case 2:
                    colors[0] = colors[1] = "#FFD166";
                    label = "Fair — add a number or symbol";
                    txtStrength.Foreground = Brush("#FFD166");
                    break;
                case 3:
                    colors[0] = colors[1] = colors[2] = "#6C63FF";
                    label = "Good — add a symbol for best security";
                    txtStrength.Foreground = Brush("#6C63FF");
                    break;
                case 4:
                    colors[0] = colors[1] = colors[2] = colors[3] = "#2CB67D";
                    label = "Strong ✓";
                    txtStrength.Foreground = Brush("#2CB67D");
                    break;
            }

            s1.Background = Brush(colors[0]);
            s2.Background = Brush(colors[1]);
            s3.Background = Brush(colors[2]);
            s4.Background = Brush(colors[3]);
            txtStrength.Text = label;
        }

        // ══════════════════════════════════════
        // REGISTER BUTTON
        // ══════════════════════════════════════
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            HideAllErrors();
            bool hasErrors = false;

            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = _showPassword
                              ? txtPasswordVisible.Text
                              : txtPassword.Password;
            string confirmPassword = _showConfirm
                              ? txtConfirmVisible.Text
                              : txtConfirmPassword.Password;
            string role = (cmbRole.SelectedItem as
                System.Windows.Controls.ComboBoxItem)
                ?.Content?.ToString() ?? "User";

            // ── Full Name Validation ──
            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowError(errFullName, "Full name is required");
                hasErrors = true;
            }
            else if (fullName.Length < 3)
            {
                ShowError(errFullName,
                    "Full name must be at least 3 characters");
                hasErrors = true;
            }
            else if (!Regex.IsMatch(fullName, @"^[a-zA-Z\s]+$"))
            {
                ShowError(errFullName,
                    "Full name can only contain letters and spaces");
                hasErrors = true;
            }

            // ── Email Validation ──
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError(errEmail, "Email address is required");
                hasErrors = true;
            }
            else if (!Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase))
            {
                ShowError(errEmail,
                    "Enter a valid email (e.g. name@email.com)");
                hasErrors = true;
            }
            else if (_context.Users.Any(u => u.Email == email))
            {
                ShowError(errEmail,
                    "This email is already registered");
                hasErrors = true;
            }

            // ── Password Validation ──
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError(errPassword, "Password is required");
                hasErrors = true;
            }
            else if (password.Length < 8)
            {
                ShowError(errPassword,
                    "Password must be at least 8 characters");
                hasErrors = true;
            }
            else if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                ShowError(errPassword,
                    "Must contain at least one uppercase letter");
                hasErrors = true;
            }
            else if (!Regex.IsMatch(password, @"[0-9]"))
            {
                ShowError(errPassword,
                    "Must contain at least one number");
                hasErrors = true;
            }

            // ── Confirm Password Validation ──
            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                ShowError(errConfirmPassword,
                    "Please confirm your password");
                hasErrors = true;
            }
            else if (password != confirmPassword)
            {
                ShowError(errConfirmPassword,
                    "Passwords do not match");
                hasErrors = true;
            }

            if (hasErrors) return;

            // ══════════════════════════════════════
            // SAVE USER TO DATABASE
            // ══════════════════════════════════════
            try
            {
                btnRegister.IsEnabled = false;
                btnRegister.Content = "Creating account...";

                var newUser = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = role
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                new ActivityLogger(_context).Log(
                    newUser.ID,
                    $"New user registered: {fullName}");

                // ══════════════════════════════════════
                // SHOW SUCCESS THEN GO TO LOGIN
                // ══════════════════════════════════════
                borderSuccess.Visibility = Visibility.Visible;

                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1.5)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    new LoginWindow().Show();
                    Close();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                txtError.Text = $"Registration failed: {ex.Message}";
                borderError.Visibility = Visibility.Visible;
            }
            finally
            {
                btnRegister.IsEnabled = true;
                btnRegister.Content = "Create Account";
            }
        }

        // ══════════════════════════════════════
        // NAVIGATION
        // ══════════════════════════════════════
        private void Login_Click(object sender,
            MouseButtonEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        // ══════════════════════════════════════
        // HELPER METHODS
        // ══════════════════════════════════════
        private void ShowError(
            System.Windows.Controls.TextBlock block,
            string msg)
        {
            block.Text = msg;
            block.Visibility = Visibility.Visible;
        }

        private void HideAllErrors()
        {
            errFullName.Visibility = Visibility.Collapsed;
            errEmail.Visibility = Visibility.Collapsed;
            errPassword.Visibility = Visibility.Collapsed;
            errConfirmPassword.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;
            borderSuccess.Visibility = Visibility.Collapsed;
        }

        private SolidColorBrush Brush(string hex) =>
            new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(hex));
    }
}