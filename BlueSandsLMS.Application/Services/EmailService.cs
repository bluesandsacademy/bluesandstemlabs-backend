using BlueSandsLMS.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlueSandsLMS.Application.ServicesF
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        public EmailService(ILogger<EmailService> logger) => _logger = logger;

        public Task SendAsync(string to, string subject, string htmlBody, string? fromEmail = null, string? fromName = null)
        {
            _logger.LogInformation("Email -> {To}\nSUBJECT: {Subject}\nFROM: {From}\nBODY:\n{Body}",
                to, subject, fromEmail ?? "(default sender)", htmlBody);
            return Task.CompletedTask;
        }
    }
}
