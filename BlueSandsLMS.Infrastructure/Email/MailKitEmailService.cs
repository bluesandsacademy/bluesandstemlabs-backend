using BlueSandsLMS.Common.Interfaces;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net.Sockets;

namespace BlueSandsLMS.Infrastructure.Email;

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
        var pass = es["FromPassword"];
        var resolvedFromEmail = fromEmail ?? authUser ?? "noreply@bluesandstemlabs.com";
        var resolvedFromName = fromName ?? es["FromDisplayName"] ?? "Blue Sands STEM Labs";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(resolvedFromName, resolvedFromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new MailKit.Net.Smtp.SmtpClient();

        // Respect a configurable timeout (seconds)
        var timeoutSeconds = es.GetValue<int?>("ConnectTimeoutSeconds") ?? 10;

        try
        {
            // The socket options MUST match the port, not just the EnableSsl flag.
            // 465 = implicit TLS from the first byte (SslOnConnect).
            // 587 = plaintext connect, then STARTTLS upgrade.
            // Mismatching these causes MailKit to hang waiting for a response
            // that will never arrive in the expected format, which surfaces as
            // a TaskCanceledException inside ReadResponseAsync rather than a
            // clean connection failure.
            var socketOptions = !enableSsl
                ? SecureSocketOptions.None
                : port switch
                {
                    465 => SecureSocketOptions.SslOnConnect,
                    587 => SecureSocketOptions.StartTls,
                    25 => SecureSocketOptions.StartTlsWhenAvailable,
                    _ => SecureSocketOptions.Auto
                };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            await client.ConnectAsync(host, port, socketOptions, cts.Token);

            if (!string.IsNullOrWhiteSpace(authUser))
            {
                // Authenticate only when credentials are configured
                await client.AuthenticateAsync(authUser, pass, cts.Token);
            }

            await client.SendAsync(message, cts.Token);
            await client.DisconnectAsync(true, CancellationToken.None);

            _logger.LogInformation("Email sent to {To} via {Host}:{Port} ({SocketOptions})", to, host, port, socketOptions);
        }
        catch (OperationCanceledException oce)
        {
            _logger.LogError(oce,
                "Timeout ({Timeout}s) talking to SMTP server {Host}:{Port}. " +
                "This can mean the port is blocked outbound, or the TLS mode doesn't match the port " +
                "(465 needs SslOnConnect, 587 needs StartTls). Falling back to logging the email.",
                timeoutSeconds, host, port);
            LogEmailFallback(to, subject, htmlBody, resolvedFromEmail, resolvedFromName);
        }
        catch (SocketException se)
        {
            _logger.LogError(se, "Socket error connecting to SMTP server {Host}:{Port}. Falling back to logging the email. Check network / SMTP host settings.", host, port);
            LogEmailFallback(to, subject, htmlBody, resolvedFromEmail, resolvedFromName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via {Host}:{Port}. Falling back to logging the email.", to, host, port);
            LogEmailFallback(to, subject, htmlBody, resolvedFromEmail, resolvedFromName);
        }
    }

    private void LogEmailFallback(string to, string subject, string htmlBody, string fromEmail, string fromName)
    {
        // Safe fallback during development / unreachable SMTP hosts: log full details so messages aren't lost.
        _logger.LogWarning("=== Email fallback (not sent) ===\nTO: {To}\nFROM: {FromName} <{From}>\nSUBJECT: {Subject}\nBODY:\n{Body}\n=== End Email ===",
            to, fromName, fromEmail, subject, htmlBody);
    }
}