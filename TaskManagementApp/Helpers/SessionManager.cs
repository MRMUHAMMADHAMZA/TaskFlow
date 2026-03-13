using TaskManagementApp.Models;

namespace TaskManagementApp.Helpers
{
    public static class SessionManager
    {
        // ── Logged-in user ──
        public static User? CurrentUser { get; set; }
        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin =>
            CurrentUser?.Role?.ToLower() == "admin";

        // ── OTP ──
        public static string? PendingOTP { get; set; }
        public static string? PendingEmail { get; set; }
        public static string? PendingUserName { get; set; }
        public static DateTime? OTPExpiry { get; set; }

        // ── Login ──
        public static void Login(User user)
        {
            CurrentUser = user;
        }

        // ── Logout ──
        public static void Logout()
        {
            CurrentUser = null;
            ClearOTP();
        }

        // ── OTP Helpers ──
        public static bool IsOTPValid(string enteredOTP)
        {
            if (PendingOTP == null || OTPExpiry == null)
                return false;

            return enteredOTP == PendingOTP
                   && DateTime.Now <= OTPExpiry.Value;
        }

        public static void ClearOTP()
        {
            PendingOTP = null;
            PendingEmail = null;
            PendingUserName = null;
            OTPExpiry = null;
        }
    }
}