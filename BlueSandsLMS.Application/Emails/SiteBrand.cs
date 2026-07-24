using Microsoft.Extensions.Configuration;

namespace BlueSandsLMS.Application.Emails
{

    public sealed record SiteBrand(
        string AppName,
        string FrontendBaseUrl,
        string SupportEmail,
        string SupportPhone,
        string FromEmail,
        string FromDisplayName,
        string SiteKey
    );

    public static class SiteBrandResolver
    {

        public static SiteBrand Resolve(string? origin, IConfiguration config) =>
            BlueSandsBrand(config);

        public static SiteBrand ResolveByKey(string? siteKey, IConfiguration config) =>
            BlueSandsBrand(config);

        private static SiteBrand BlueSandsBrand(IConfiguration config) => new(
            AppName:         "Blue Sands STEM Labs",
            FrontendBaseUrl: (config["App:FrontendBaseUrl"] ?? "https://app.bluesandstemlabs.com").TrimEnd('/'),
            SupportEmail:    config["App:SupportEmail"]  ?? "support@bluesandstemlabs.com",
            SupportPhone:    config["App:SupportPhone"]  ?? "+234 7034194669",
            FromEmail:       config["EmailSettings:FromEmail"]       ?? "noreply@bluesandstemlabs.com",
            FromDisplayName: config["EmailSettings:FromDisplayName"] ?? "Blue Sands STEM Labs",
            SiteKey:         "bluesands"
        );
    }
}
