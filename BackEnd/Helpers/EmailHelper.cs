using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Backend.Interfaces;
using Backend.Services;

namespace Backend.Helpers
{
    public static class EmailHelper
    {
        public static async Task SendVerificationEmailAsync(IEmailService emailService, string to, string firstName, string verificationUrl)
        {
            var subject = "Verify Your Email - TasksAPI";
            
            var html = $@"
<html>
<body style=""font-family: Arial, sans-serif; padding: 20px; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border: 1px solid #ddd;"">
        <h2>Welcome to TasksAPI!</h2>
        <p>Hi {firstName},</p>
        <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
        <p style=""margin: 30px 0;"">
            <a href=""{verificationUrl}"" style=""display: inline-block; padding: 12px 24px; background-color: #667eea; color: white; text-decoration: none; border-radius: 4px;"">Verify Email Address</a>
        </p>
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; color: #667eea;"">{verificationUrl}</p>
        <p style=""color: #666; font-size: 14px; margin-top: 30px;"">If you didn't create this account, please ignore this email.</p>
        <hr style=""border: none; border-top: 1px solid #ddd; margin: 20px 0;""/>
        <p style=""color: #999; font-size: 12px; text-align: center;"">TasksAPI &copy; 2024</p>
    </div>
</body>
</html>";

            await emailService.SendEmailAsync(to, subject, html);
        }

        public static async Task SendPasswordResetEmailAsync(IEmailService emailService, string to, string firstName, string resetUrl)
        {
            var subject = "Reset Your Password - TasksAPI";
            
            var html = $@"
<html>
<body style=""font-family: Arial, sans-serif; padding: 20px; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border: 1px solid #ddd;"">
        <h2>Password Reset Request</h2>
        <p>Hi {firstName},</p>
        <p>You requested to reset your password. Click the link below to proceed:</p>
        <p style=""margin: 30px 0;"">
            <a href=""{resetUrl}"" style=""display: inline-block; padding: 12px 24px; background-color: #667eea; color: white; text-decoration: none; border-radius: 4px;"">Reset Password</a>
        </p>
        <p style=""padding: 10px; background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 4px;"">
            <strong>Note:</strong> This link will expire in 1 hour.
        </p>
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; color: #667eea;"">{resetUrl}</p>
        <p style=""color: #666; font-size: 14px; margin-top: 30px;"">If you didn't request this, please ignore this email.</p>
        <hr style=""border: none; border-top: 1px solid #ddd; margin: 20px 0;""/>
        <p style=""color: #999; font-size: 12px; text-align: center;"">TasksAPI &copy; 2024</p>
    </div>
</body>
</html>";

            await emailService.SendEmailAsync(to, subject, html);
        }

        public static async Task SendAlreadyRegisteredEmailAsync(IEmailService emailService, string email, string loginUrl, string resetUrl)
        {
            var subject = "Account Already Exists - TasksAPI";
            
            var html = $@"
<html>
<body style=""font-family: Arial, sans-serif; padding: 20px; background-color: #f5f5f5;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border: 1px solid #ddd;"">
        <h2>Account Already Exists</h2>
        <p>Hello,</p>
        <p>We received a registration attempt for this email address, but an account already exists.</p>
        <p>If this was you, you can:</p>
        <p style=""margin: 20px 0;"">
            <a href=""{loginUrl}"" style=""display: inline-block; padding: 12px 24px; background-color: #667eea; color: white; text-decoration: none; border-radius: 4px; margin-right: 10px;"">Log In</a>
            <a href=""{resetUrl}"" style=""display: inline-block; padding: 12px 24px; background-color: white; color: #667eea; text-decoration: none; border-radius: 4px; border: 2px solid #667eea;"">Reset Password</a>
        </p>
        <p style=""color: #666; font-size: 14px; margin-top: 30px;"">If you didn't attempt to register, you can safely ignore this email.</p>
        <hr style=""border: none; border-top: 1px solid #ddd; margin: 20px 0;""/>
        <p style=""color: #999; font-size: 12px; text-align: center;"">TasksAPI &copy; 2024</p>
    </div>
</body>
</html>";

            await emailService.SendEmailAsync(email, subject, html);
        }
    }
}