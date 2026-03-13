using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TaskManagementApp.Data;
using TaskManagementApp.Helpers;

namespace TaskManagementApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AppDbContext _context;
        private bool _showPassword = false;

        public LoginWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
        }

        // ══════════════════════════════════════
        // CLEAR ERRORS WHILE TYPING
        // ══════════════════════════════════════
        private void txtEmail_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            errEmail.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;
        }

        private void txtPassword_PasswordChanged(object sender,
            RoutedEventArgs e)
        {
            errPassword.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;
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
        // LOGIN BUTTON — ASYNC FOR OTP EMAIL
        // ══════════════════════════════════════
        private async void btnLogin_Click(object sender,
            RoutedEventArgs e)
        {
            HideAllErrors();

            string email = txtEmail.Text.Trim();
            string password = _showPassword
                              ? txtPasswordVisible.Text
                              : txtPassword.Password;

            bool hasErrors = false;

            // ── Validation ──
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowFieldError(errEmail, "Email address is required");
                hasErrors = true;
            }
            else if (!IsValidEmail(email))
            {
                ShowFieldError(errEmail,
                    "Please enter a valid email address");
                hasErrors = true;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowFieldError(errPassword, "Password is required");
                hasErrors = true;
            }

            if (hasErrors) return;

            // ── Authentication ──
            try
            {
                btnLogin.IsEnabled = false;
                btnLogin.Content = "Signing in...";

                var user = _context.Users
                    .FirstOrDefault(u => u.Email == email);

                if (user == null ||
                    !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    ShowGeneralError(
                        "Invalid email or password. Please try again.");
                    return;
                }

                // ── Password correct — Generate & Send OTP ──
                btnLogin.Content = "Sending verification code...";

                var random = new Random();
                string otp = random.Next(100000, 999999).ToString();

                SessionManager.CurrentUser = user;
                SessionManager.PendingOTP = otp;
                SessionManager.PendingEmail = user.Email;
                SessionManager.PendingUserName = user.FullName;
                SessionManager.OTPExpiry = DateTime.Now.AddMinutes(5);

                bool emailSent = await Task.Run(() =>
                    EmailService.SendOTPEmail(
                        user.Email,
                        user.FullName,
                        otp));

                if (!emailSent)
                {
                    SessionManager.CurrentUser = null;
                    SessionManager.ClearOTP();
                    ShowGeneralError(
                        "Failed to send verification code. " +
                        "Please try again.");
                    return;
                }

                new ActivityLogger(_context).Log(
                    user.ID,
                    $"User {user.FullName} initiated 2FA login");

                new OTPWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                ShowGeneralError($"Error: {ex.Message}");
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "Sign In";
            }
        }

        // ══════════════════════════════════════
        // REGISTER LINK
        // ══════════════════════════════════════
        private void Register_Click(object sender,
            MouseButtonEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }

        // ══════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════
        private bool IsValidEmail(string email) =>
            Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);

        private void ShowFieldError(
            System.Windows.Controls.TextBlock block,
            string message)
        {
            block.Text = message;
            block.Visibility = Visibility.Visible;
        }

        private void ShowGeneralError(string message)
        {
            txtError.Text = message;
            borderError.Visibility = Visibility.Visible;
        }

        private void HideAllErrors()
        {
            errEmail.Visibility = Visibility.Collapsed;
            errPassword.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;
        }
    }
}