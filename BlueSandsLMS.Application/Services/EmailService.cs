using BlueSandsLMS.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlueSandsLMS.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        public EmailService(ILogger<EmailService> logger) => _logger = logger;

        public Task SendAsync(string to, string subject, string htmlBody, string? from = null)
        {
            _logger.LogInformation("Email -> {To}\nSUBJECT: {Subject}\nFROM: {From}\nBODY:\n{Body}",
                to, subject, from ?? "(default sender)", htmlBody);
            return Task.CompletedTask;
        }
    }
}
