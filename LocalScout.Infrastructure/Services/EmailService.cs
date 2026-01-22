using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace LocalScout.Infrastructure.Services
{
    public class EmailService : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var settings = _configuration.GetSection("EmailSettings");

                // Get values with defaults
                var smtpServer = settings["SmtpServer"] ?? throw new ArgumentNullException("SmtpServer is required");
                var port = int.Parse(settings["SmtpPort"] ?? "587");
                var username = settings["SmtpUsername"] ?? throw new ArgumentNullException("SmtpUsername is required");
                var password = settings["SmtpPassword"] ?? throw new ArgumentNullException("SmtpPassword is required");
                var fromEmail = settings["FromEmail"] ?? username;
                var fromName = settings["FromName"] ?? "Neighbourly";
                var enableSsl = bool.Parse(settings["EnableSsl"] ?? "true");

                using var client = new SmtpClient(smtpServer, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(username, password)
                };

                var fromAddress = new MailAddress(fromEmail, fromName);
                using var mailMessage = new MailMessage()
                {
                    From = fromAddress,
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - allows registration to continue even if email fails
                Console.WriteLine($"Failed to send email: {ex.Message}");
                throw; // Re-throw if you want to handle it in the calling code
            }
        }

        /// <summary>
        /// Generates a professional email confirmation template
        /// </summary>
        public static string GetConfirmationEmailTemplate(string userName, string confirmationLink, string userType = "User")
        {
            var welcomeTitle = userType == "Provider" 
                ? "Welcome to Neighbourly as a Service Provider!" 
                : "Welcome to Neighbourly";
            
            var welcomeMessage = userType == "Provider"
                ? "Thank you for joining Neighbourly as a service provider. You're one step away from connecting with customers in your area and growing your business."
                : "Thank you for joining Neighbourly You're one step away from discovering trusted local services in your neighborhood.";

            return $@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Confirm Your Email - Neighbourly</title>
            </head>
            <body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
                <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                    <tr>
                        <td align=""center"" style=""padding: 40px 0;"">
                            <table role=""presentation"" style=""width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 16px; box-shadow: 0 4px 24px rgba(0, 0, 0, 0.08);"">
                    
                                <!-- Header -->
                                <tr>
                                    <td style=""padding: 40px 40px 30px; text-align: center; background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); border-radius: 16px 16px 0 0;"">
                                        <h1 style=""margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;"">
                                            🏠 Neighbourly
                                        </h1>
                                        <p style=""margin: 10px 0 0; color: rgba(255, 255, 255, 0.9); font-size: 14px;"">
                                            Your Trusted Local Services Marketplace
                                        </p>
                                    </td>
                                </tr>

                                <!-- Main Content -->
                                <tr>
                                    <td style=""padding: 40px;"">
                                        <!-- Welcome Section -->
                                        <h2 style=""margin: 0 0 20px; color: #1e293b; font-size: 24px; font-weight: 600;"">
                                            {welcomeTitle}
                                        </h2>
                            
                                        <p style=""margin: 0 0 15px; color: #475569; font-size: 16px; line-height: 1.6;"">
                                            Hi <strong style=""color: #1e293b;"">{userName}</strong>,
                                        </p>
                            
                                        <p style=""margin: 0 0 30px; color: #475569; font-size: 16px; line-height: 1.6;"">
                                            {welcomeMessage}
                                        </p>

                                        <!-- CTA Button -->
                                        <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                                            <tr>
                                                <td align=""center"" style=""padding: 10px 0 30px;"">
                                                    <a href=""{confirmationLink}"" 
                                                       style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); color: #ffffff; text-decoration: none; font-size: 16px; font-weight: 600; border-radius: 10px; box-shadow: 0 4px 14px rgba(59, 130, 246, 0.4); transition: all 0.3s ease;"">
                                                        ✉️ Confirm My Email
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Alternative Link -->
                                        <div style=""background-color: #f8fafc; border-radius: 10px; padding: 20px; margin-bottom: 30px;"">
                                            <p style=""margin: 0 0 10px; color: #64748b; font-size: 14px;"">
                                                If the button doesn't work, copy and paste this link into your browser:
                                            </p>
                                            <p style=""margin: 0; word-break: break-all;"">
                                                <a href=""{confirmationLink}"" style=""color: #3b82f6; font-size: 13px; text-decoration: underline;"">
                                                    {confirmationLink}
                                                </a>
                                            </p>
                                        </div>

                                        <!-- Security Notice -->
                                        <div style=""border-left: 4px solid #f59e0b; background-color: #fffbeb; padding: 15px 20px; border-radius: 0 8px 8px 0; margin-bottom: 20px;"">
                                            <p style=""margin: 0; color: #92400e; font-size: 14px; line-height: 1.5;"">
                                                <strong>🔒 Security Notice:</strong> This link will expire in 24 hours. If you didn't create an account with Neighbourly, please ignore this email.
                                            </p>
                                        </div>
                                    </td>
                                </tr>

                                <!-- What's Next Section -->
                                <tr>
                                    <td style=""padding: 0 40px 40px;"">
                                        <h3 style=""margin: 0 0 20px; color: #1e293b; font-size: 18px; font-weight: 600;"">
                                            What's Next?
                                        </h3>
                            
                                        <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                                            <tr>
                                                <td style=""padding: 12px 0; border-bottom: 1px solid #e2e8f0;"">
                                                    <table role=""presentation"" style=""width: 100%;"">
                                                        <tr>
                                                            <td style=""width: 40px; vertical-align: top;"">
                                                                <span style=""display: inline-block; width: 28px; height: 28px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border-radius: 50%; text-align: center; line-height: 28px; font-size: 14px; font-weight: 600;"">1</span>
                                                            </td>
                                                            <td style=""color: #475569; font-size: 15px; line-height: 1.5;"">
                                                                <strong style=""color: #1e293b;"">Confirm your email</strong> by clicking the button above
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style=""padding: 12px 0; border-bottom: 1px solid #e2e8f0;"">
                                                    <table role=""presentation"" style=""width: 100%;"">
                                                        <tr>
                                                            <td style=""width: 40px; vertical-align: top;"">
                                                                <span style=""display: inline-block; width: 28px; height: 28px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border-radius: 50%; text-align: center; line-height: 28px; font-size: 14px; font-weight: 600;"">2</span>
                                                            </td>
                                                            <td style=""color: #475569; font-size: 15px; line-height: 1.5;"">
                                                                <strong style=""color: #1e293b;"">Complete your profile</strong> to get personalized recommendations
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style=""padding: 12px 0;"">
                                                    <table role=""presentation"" style=""width: 100%;"">
                                                        <tr>
                                                            <td style=""width: 40px; vertical-align: top;"">
                                                                <span style=""display: inline-block; width: 28px; height: 28px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; border-radius: 50%; text-align: center; line-height: 28px; font-size: 14px; font-weight: 600;"">3</span>
                                                            </td>
                                                            <td style=""color: #475569; font-size: 15px; line-height: 1.5;"">
                                                                <strong style=""color: #1e293b;"">Start exploring</strong> trusted local services near you
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style=""padding: 30px 40px; background-color: #f8fafc; border-radius: 0 0 16px 16px; text-align: center;"">
                                        <p style=""margin: 0 0 15px; color: #64748b; font-size: 14px;"">
                                            Need help? Contact us at <a href=""mailto:support@Neighbourly.com"" style=""color: #3b82f6; text-decoration: none;"">support@Neighbourly.com</a>
                                        </p>
                                        <p style=""margin: 0 0 15px; color: #94a3b8; font-size: 13px;"">
                                            © {DateTime.Now.Year} Neighbourly. All rights reserved.
                                        </p>
                                        <p style=""margin: 0; color: #cbd5e1; font-size: 12px;"">
                                            You're receiving this email because you signed up for Neighbourly.
                                        </p>
                                    </td>
                                </tr>

                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";
        }

        /// <summary>
        /// Generates a password reset email template
        /// </summary>
        public static string GetPasswordResetEmailTemplate(string userName, string resetLink)
        {
            return $@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Reset Your Password - Neighbourly</title>
            </head>
            <body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa;"">
                <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                    <tr>
                        <td align=""center"" style=""padding: 40px 0;"">
                            <table role=""presentation"" style=""width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 16px; box-shadow: 0 4px 24px rgba(0, 0, 0, 0.08);"">
                    
                                <!-- Header -->
                                <tr>
                                    <td style=""padding: 40px 40px 30px; text-align: center; background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); border-radius: 16px 16px 0 0;"">
                                        <h1 style=""margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;"">
                                            🏠 Neighbourly
                                        </h1>
                                        <p style=""margin: 10px 0 0; color: rgba(255, 255, 255, 0.9); font-size: 14px;"">
                                            Your Trusted Local Services Marketplace
                                        </p>
                                    </td>
                                </tr>

                                <!-- Main Content -->
                                <tr>
                                    <td style=""padding: 40px;"">
                                        <h2 style=""margin: 0 0 20px; color: #1e293b; font-size: 24px; font-weight: 600;"">
                                            🔐 Password Reset Request
                                        </h2>
                            
                                        <p style=""margin: 0 0 15px; color: #475569; font-size: 16px; line-height: 1.6;"">
                                            Hi <strong style=""color: #1e293b;"">{userName}</strong>,
                                        </p>
                            
                                        <p style=""margin: 0 0 30px; color: #475569; font-size: 16px; line-height: 1.6;"">
                                            We received a request to reset your password. Click the button below to create a new password:
                                        </p>

                                        <!-- CTA Button -->
                                        <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
                                            <tr>
                                                <td align=""center"" style=""padding: 10px 0 30px;"">
                                                    <a href=""{resetLink}"" 
                                                       style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); color: #ffffff; text-decoration: none; font-size: 16px; font-weight: 600; border-radius: 10px; box-shadow: 0 4px 14px rgba(59, 130, 246, 0.4);"">
                                                        🔑 Reset My Password
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Alternative Link -->
                                        <div style=""background-color: #f8fafc; border-radius: 10px; padding: 20px; margin-bottom: 30px;"">
                                            <p style=""margin: 0 0 10px; color: #64748b; font-size: 14px;"">
                                                If the button doesn't work, copy and paste this link into your browser:
                                            </p>
                                            <p style=""margin: 0; word-break: break-all;"">
                                                <a href=""{resetLink}"" style=""color: #3b82f6; font-size: 13px; text-decoration: underline;"">
                                                    {resetLink}
                                                </a>
                                            </p>
                                        </div>

                                        <!-- Security Notice -->
                                        <div style=""border-left: 4px solid #ef4444; background-color: #fef2f2; padding: 15px 20px; border-radius: 0 8px 8px 0;"">
                                            <p style=""margin: 0; color: #991b1b; font-size: 14px; line-height: 1.5;"">
                                                <strong>⚠️ Important:</strong> This link will expire in 1 hour. If you didn't request a password reset, please ignore this email or contact support if you have concerns.
                                            </p>
                                        </div>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style=""padding: 30px 40px; background-color: #f8fafc; border-radius: 0 0 16px 16px; text-align: center;"">
                                        <p style=""margin: 0 0 15px; color: #64748b; font-size: 14px;"">
                                            Need help? Contact us at <a href=""mailto:support@Neighbourly.com"" style=""color: #3b82f6; text-decoration: none;"">support@Neighbourly.com</a>
                                        </p>
                                        <p style=""margin: 0 0 15px; color: #94a3b8; font-size: 13px;"">
                                            © {DateTime.Now.Year} Neighbourly. All rights reserved.
                                        </p>
                                        <p style=""margin: 0; color: #cbd5e1; font-size: 12px;"">
                                            You're receiving this email because a password reset was requested for your account.
                                        </p>
                                    </td>
                                </tr>

                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";
        }
    }
}
