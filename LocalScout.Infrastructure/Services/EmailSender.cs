using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace LocalScout.Infrastructure.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
   {
            _logger = logger;
}

  public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
 // TODO: Implement actual email sending logic (e.g., using SendGrid, SMTP, etc.)
   // For now, just log the email details for development
      _logger.LogInformation("Email would be sent to {Email} with subject '{Subject}'", email, subject);
            _logger.LogDebug("Email body: {HtmlMessage}", htmlMessage);
   
            return Task.CompletedTask;
      }
  }
}
