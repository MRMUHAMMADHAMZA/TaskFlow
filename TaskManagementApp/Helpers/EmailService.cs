using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TaskManagementApp.Helpers
{
    public class EmailService
    {
        // ══ REPLACE WITH YOUR GMAIL DETAILS ══
        private const string SenderEmail = "mrmuhammadhamzahussain@gmail.com";
        private const string SenderPassword = "nybd iymv lyxw ngao";
        private const string SenderName = "TaskFlow";

        public static bool SendOTPEmail(string toEmail,
                                        string userName,
                                        string otp)
        {
            try
            {
                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(SenderName, SenderEmail));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = "Your TaskFlow Verification Code";

                // Build HTML email body
                message.Body = new TextPart("html")
                {
                    Text = BuildEmailTemplate(userName, otp)
                };

                using var client = new SmtpClient();

                // Connect to Gmail SMTP
                client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                client.Authenticate(SenderEmail, SenderPassword);
                client.Send(message);
                client.Disconnect(true);

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Email error: {ex.Message}",
                    "Email Failed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        private static string BuildEmailTemplate(string userName, string otp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            background-color: #0F0E17;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 500px;
            margin: 40px auto;
            background-color: #16213E;
            border-radius: 16px;
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #6C63FF, #4A42D0);
            padding: 35px 30px;
            text-align: center;
        }}
        .header h1 {{
            color: white;
            margin: 0;
            font-size: 28px;
            font-weight: 700;
        }}
        .header p {{
            color: #D4D0FF;
            margin: 8px 0 0 0;
            font-size: 14px;
        }}
        .body {{
            padding: 35px 30px;
        }}
        .greeting {{
            color: #FFFFFE;
            font-size: 16px;
            margin-bottom: 16px;
        }}
        .message {{
            color: #A7A9BE;
            font-size: 14px;
            line-height: 1.6;
            margin-bottom: 30px;
        }}
        .otp-label {{
            color: #A7A9BE;
            font-size: 12px;
            font-weight: 600;
            letter-spacing: 1px;
            text-transform: uppercase;
            margin-bottom: 12px;
        }}
        .otp-box {{
            background: #1E1E3F;
            border: 2px solid #6C63FF;
            border-radius: 12px;
            padding: 20px;
            text-align: center;
            margin-bottom: 25px;
        }}
        .otp-code {{
            color: #FFD166;
            font-size: 42px;
            font-weight: 700;
            letter-spacing: 12px;
            margin: 0;
        }}
        .expiry {{
            color: #FF6584;
            font-size: 13px;
            text-align: center;
            margin-bottom: 25px;
        }}
        .warning {{
            background: #1A1A3F;
            border-left: 3px solid #6C63FF;
            padding: 12px 16px;
            border-radius: 6px;
            color: #A7A9BE;
            font-size: 13px;
            margin-bottom: 25px;
        }}
        .footer {{
            background: #0F0E17;
            padding: 20px 30px;
            text-align: center;
            color: #3A3A5C;
            font-size: 12px;
        }}
        .footer a {{
            color: #6C63FF;
            text-decoration: none;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ TaskFlow</h1>
            <p>Email Verification</p>
        </div>
        <div class='body'>
            <p class='greeting'>Hi <strong style='color:#6C63FF'>{userName}</strong>! 👋</p>
            <p class='message'>
                Thank you for registering with TaskFlow. 
                Use the verification code below to complete 
                your account setup.
            </p>

            <p class='otp-label'>Your verification code</p>
            <div class='otp-box'>
                <p class='otp-code'>{otp[0]} {otp[1]} {otp[2]} {otp[3]} {otp[4]} {otp[5]}</p>
            </div>

            <p class='expiry'>⏱ This code expires in <strong>5 minutes</strong></p>

            <div class='warning'>
                🔒 <strong style='color:#FFFFFE'>Security notice:</strong> 
                Never share this code with anyone. 
                TaskFlow will never ask for your OTP.
            </div>

            <p class='message'>
                If you didn't create a TaskFlow account, 
                you can safely ignore this email.
            </p>
        </div>
        <div class='footer'>
            © 2026 TaskFlow · 
            <a href='#'>Privacy Policy</a> · 
            <a href='#'>Terms of Service</a>
        </div>
    </div>
</body>
</html>";
        }
    }
}