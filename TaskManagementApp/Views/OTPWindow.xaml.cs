using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TaskManagementApp.Data;
using TaskManagementApp.Helpers;

namespace TaskManagementApp.Views
{
    public partial class OTPWindow : Window
    {
        private DispatcherTimer? _countdownTimer;
        private DateTime _expiryTime;
        private TextBox[] _otpBoxes;
        private int _failedAttempts = 0;
        private const int MaxAttempts = 3;

        public OTPWindow()
        {
            InitializeComponent();
            _otpBoxes = new[] { otp1, otp2, otp3, otp4, otp5, otp6 };
            LoadOTPInfo();
            StartCountdown();
            otp1.Focus();
        }

        // ══════════════════════════════════════
        // LOAD INFO FROM SESSION
        // ══════════════════════════════════════
        private void LoadOTPInfo()
        {
            string email = SessionManager.PendingEmail ?? "";
            string name = SessionManager.PendingUserName ?? "User";

            string maskedEmail = MaskEmail(email);
            txtEmailHint.Text = $"We sent a verification code to\n{maskedEmail}";
            txtSubtitle.Text = $"Hi {name}! Enter the 6-digit code " +
                                    $"sent to {maskedEmail}";
        }

        // ══════════════════════════════════════
        // COUNTDOWN TIMER
        // ══════════════════════════════════════
        private void StartCountdown()
        {
            _expiryTime = SessionManager.OTPExpiry ?? DateTime.Now;
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
            UpdateTimerDisplay();
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            var remaining = _expiryTime - DateTime.Now;

            if (remaining.TotalSeconds <= 0)
            {
                _countdownTimer.Stop();
                txtTimer.Text = "Expired!";
                txtTimer.Foreground = new SolidColorBrush(Colors.Red);
                btnVerify.IsEnabled = false;
                ShowError("⏱ OTP has expired. Please click Resend " +
                          "to get a new code.");
                return;
            }

            txtTimer.Text = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";

            txtTimer.Foreground = remaining.TotalSeconds <= 60
                ? new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF6584"))
                : new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFD166"));
        }

        // ══════════════════════════════════════
        // OTP BOX — NUMBERS ONLY
        // ══════════════════════════════════════
        private void OTPBox_PreviewTextInput(object sender,
            TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text[0]);
        }

        // ══════════════════════════════════════
        // AUTO JUMP TO NEXT BOX
        // ══════════════════════════════════════
        private void otp1_TextChanged(object s, TextChangedEventArgs e)
        { if (otp1.Text.Length == 1) otp2.Focus(); HideError(); }

        private void otp2_TextChanged(object s, TextChangedEventArgs e)
        { if (otp2.Text.Length == 1) otp3.Focus(); HideError(); }

        private void otp3_TextChanged(object s, TextChangedEventArgs e)
        { if (otp3.Text.Length == 1) otp4.Focus(); HideError(); }

        private void otp4_TextChanged(object s, TextChangedEventArgs e)
        { if (otp4.Text.Length == 1) otp5.Focus(); HideError(); }

        private void otp5_TextChanged(object s, TextChangedEventArgs e)
        { if (otp5.Text.Length == 1) otp6.Focus(); HideError(); }

        private void otp6_TextChanged(object s, TextChangedEventArgs e)
        { HideError(); }

        // ══════════════════════════════════════
        // BACKSPACE — GO TO PREVIOUS BOX
        // ══════════════════════════════════════
        private void OTPBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Back) return;
            var current = sender as TextBox;
            if (current == null || current.Text.Length > 0) return;
            int index = Array.IndexOf(_otpBoxes, current);
            if (index > 0)
                _otpBoxes[index - 1].Focus();
        }

        // ══════════════════════════════════════
        // VERIFY BUTTON
        // ══════════════════════════════════════
        private void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string enteredOtp = otp1.Text + otp2.Text + otp3.Text +
                                otp4.Text + otp5.Text + otp6.Text;

            // ── Check all boxes filled ──
            if (enteredOtp.Length < 6)
            {
                ShowError("⚠ Please enter all 6 digits of your OTP.");
                HighlightEmptyBoxes();
                return;
            }

            // ── Check if expired ──
            if (DateTime.Now > SessionManager.OTPExpiry)
            {
                ShowError("⏱ OTP has expired. Please request a new one.");
                btnVerify.IsEnabled = false;
                return;
            }

            // ── Check OTP validity ──
            if (!SessionManager.IsOTPValid(enteredOtp))
            {
                _failedAttempts++;
                int remaining = MaxAttempts - _failedAttempts;

                ShakeBoxes();
                ClearBoxes();
                otp1.Focus();

                if (_failedAttempts >= MaxAttempts)
                {
                    // ── Lock after max attempts ──
                    ShowError(
                        "❌ Too many incorrect attempts. " +
                        "Please request a new OTP.");
                    btnVerify.IsEnabled = false;
                    DisableAllBoxes();
                }
                else
                {
                    // ── Show remaining attempts ──
                    ShowError(
                        $"❌ Invalid OTP. You have {remaining} " +
                        $"attempt{(remaining == 1 ? "" : "s")} remaining.");
                }
                return;
            }

            // ══════════════════════════════════════
            // OTP CORRECT — LOG & GO TO DASHBOARD
            // ══════════════════════════════════════
            _countdownTimer.Stop();
            SessionManager.ClearOTP();

            // Turn all boxes green
            foreach (var box in _otpBoxes)
                box.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#2CB67D"));

            borderSuccess.Visibility = Visibility.Visible;
            btnVerify.IsEnabled = false;

            // Log successful 2FA login
            try
            {
                using var context = new AppDbContext();
                new ActivityLogger(context).Log(
                    SessionManager.CurrentUser!.ID,
                    $"User {SessionManager.CurrentUser.FullName} " +
                    $"completed 2FA and logged in successfully");
            }
            catch { /* non-critical logging error */ }

            // Navigate to Dashboard
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                new DashboardWindow().Show();
                Close();
            };
            timer.Start();
        }

        // ══════════════════════════════════════
        // RESEND OTP
        // ══════════════════════════════════════
        private async void lnkResend_Click(object sender,
            MouseButtonEventArgs e)
        {
            // Generate new OTP
            var random = new Random();
            string newOtp = random.Next(100000, 999999).ToString();

            SessionManager.PendingOTP = newOtp;
            SessionManager.OTPExpiry = DateTime.Now.AddMinutes(5);

            // Reset failed attempts
            _failedAttempts = 0;
            btnVerify.IsEnabled = true;

            // Restart countdown
            _countdownTimer.Stop();
            StartCountdown();

            // Reset UI
            ClearBoxes();
            EnableAllBoxes();
            ResetBoxColors();
            HideError();
            borderSuccess.Visibility = Visibility.Collapsed;
            otp1.Focus();

            // Disable resend link while sending
            lnkResend.IsEnabled = false;
            lnkResend.Opacity = 0.5;

            // Send new OTP email
            bool sent = await Task.Run(() =>
                EmailService.SendOTPEmail(
                    SessionManager.PendingEmail ?? "",
                    SessionManager.PendingUserName ?? "User",
                    newOtp));

            lnkResend.IsEnabled = true;
            lnkResend.Opacity = 1;

            if (sent)
            {
                // Show success briefly
                borderSuccess.Visibility = Visibility.Visible;

                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    borderSuccess.Visibility = Visibility.Collapsed;
                };
                timer.Start();
            }
            else
            {
                ShowError("❌ Failed to resend OTP. Please try again.");
            }
        }

        // ══════════════════════════════════════
        // BACK TO LOGIN
        // ══════════════════════════════════════
        private void lnkBack_Click(object sender,
            MouseButtonEventArgs e)
        {
            _countdownTimer.Stop();
            SessionManager.ClearOTP();
            SessionManager.CurrentUser = null;
            new LoginWindow().Show();
            Close();
        }

        // ══════════════════════════════════════
        // UI HELPERS
        // ══════════════════════════════════════
        private void ShowError(string msg)
        {
            txtError.Text = msg;
            borderError.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            borderError.Visibility = Visibility.Collapsed;
        }

        private void ClearBoxes()
        {
            foreach (var box in _otpBoxes)
                box.Text = string.Empty;
        }

        private void ResetBoxColors()
        {
            foreach (var box in _otpBoxes)
                box.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#2A2A4A"));
        }

        private void HighlightEmptyBoxes()
        {
            foreach (var box in _otpBoxes)
                if (string.IsNullOrEmpty(box.Text))
                    box.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#FF6584"));
        }

        private void DisableAllBoxes()
        {
            foreach (var box in _otpBoxes)
            {
                box.IsEnabled = false;
                box.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF6584"));
            }
        }

        private void EnableAllBoxes()
        {
            foreach (var box in _otpBoxes)
                box.IsEnabled = true;
        }

        private void ShakeBoxes()
        {
            foreach (var box in _otpBoxes)
                box.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF6584"));

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                ResetBoxColors();
            };
            timer.Start();
        }

        private string MaskEmail(string email)
        {
            if (!email.Contains("@")) return email;
            var parts = email.Split('@');
            string name = parts[0];
            string masked = name.Length <= 2
                ? name
                : name[..2] + new string('*', name.Length - 2);
            return masked + "@" + parts[1];
        }
    }
}