using System.Net;
using System.Net.Mail;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueSandsLMS.Infrastructure.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, string? from = null)
        {
            var es = _config.GetSection("EmailSettings");

            var host     = es["SmtpServer"] ?? _config["Smtp:Host"] ?? "smtp.gmail.com";
            var portStr  = es["SmtpPort"]   ?? _config["Smtp:Port"] ?? "587";
            var user     = es["FromEmail"]  ?? _config["Smtp:User"];
            var passRaw  = es["FromPassword"] ?? _config["Smtp:Pass"];
            var sslStr   = es["EnableSsl"]  ?? _config["Smtp:EnableSsl"] ?? "true";

            int.TryParse(portStr, out var port);
            bool.TryParse(sslStr, out var enableSsl);

            var pass = string.IsNullOrWhiteSpace(passRaw) ? passRaw : passRaw.Replace(" ", "");

            var fromEmail = from ?? es["FromEmail"] ?? _config["Smtp:FromEmail"] ?? "noreply@bluesandstemlabs.com";
            var fromName  = es["FromDisplayName"] ?? _config["Smtp:FromName"] ?? "Blue Sands STEM Labs";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = string.IsNullOrWhiteSpace(user)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(user, pass),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(to));

            try
            {
                _logger.LogInformation("Attempting to send email to {To} via {Host}:{Port} as {User}", to, host, port, user);
                await client.SendMailAsync(mail);
                _logger.LogInformation("✅ Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send email to {To}", to);
                throw;
            }
        }
    }
}
