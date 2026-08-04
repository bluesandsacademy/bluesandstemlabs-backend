using System.Net;
using System.Net.Mail;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueSandsLMS.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? fromEmail = null, string? fromName = null)
    {
        var es = _config.GetSection("EmailSettings");

        var host     = es["SmtpServer"]  ?? "smtp.gmail.com";
        var portStr  = es["SmtpPort"]    ?? "587";
        var sslStr   = es["EnableSsl"]   ?? "true";
        var authUser = es["FromEmail"];
        var passRaw  = es["FromPassword"];

        int.TryParse(portStr, out var port);
        bool.TryParse(sslStr, out var enableSsl);
        var pass = string.IsNullOrWhiteSpace(passRaw) ? passRaw : passRaw!.Replace(" ", "");

        var resolvedFromEmail = fromEmail ?? authUser ?? "noreply@bluesandstemlabs.com";
        var resolvedFromName  = fromName  ?? es["FromDisplayName"] ?? "Blue Sands STEM Labs";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl             = enableSsl,
            UseDefaultCredentials = false,
            Credentials           = string.IsNullOrWhiteSpace(authUser)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(authUser, pass),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        using var mail = new MailMessage
        {
            From      = new MailAddress(resolvedFromEmail, resolvedFromName),
            Subject   = subject,
            Body      = htmlBody,
            IsBodyHtml = true
        };
        mail.To.Add(new MailAddress(to));

        try
        {
            _logger.LogInformation("Attempting to send email to {To} via {Host}:{Port} as {User}", to, host, port, authUser);
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
