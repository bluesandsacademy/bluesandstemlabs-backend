using BlueSandsLMS.Common.Interfaces;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BlueSandsLMS.Infrastructure.Email
{
    public class MailKitEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MailKitEmailService> _logger;

        public MailKitEmailService(IConfiguration config, ILogger<MailKitEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, string? fromEmail = null, string? fromName = null)
        {
            var es = _config.GetSection("EmailSettings");
            var host = es["SmtpServer"] ?? "smtp.gmail.com";
            var port = es.GetValue<int?>("SmtpPort") ?? 587;
            var enableSsl = es.GetValue<bool?>("EnableSsl") ?? true;
            var authUser = es["FromEmail"];
            var passRaw = es["FromPassword"];
            // var pass = string.IsNullOrWhiteSpace(passRaw) ? passRaw : passRaw!.Replace(" ", "");
            var pass = passRaw;

            var resolvedFromEmail = fromEmail ?? authUser ?? "noreply@bluesandstemlabs.com";
            var resolvedFromName = fromName ?? es["FromDisplayName"] ?? "Blue Sands STEM Labs";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(resolvedFromName, resolvedFromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                var socketOptions = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(host, port, socketOptions);
                if (!string.IsNullOrWhiteSpace(authUser))
                {
                    await client.AuthenticateAsync(authUser, pass);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email sent to {To} via {Host}:{Port}", to, host, port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }
    }
}