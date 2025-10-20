namespace BlueSandsLMS.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody, string? from = null);
    }
}
