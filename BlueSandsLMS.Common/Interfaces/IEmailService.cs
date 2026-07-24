namespace BlueSandsLMS.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody, string? fromEmail = null, string? fromName = null);
    }
}
