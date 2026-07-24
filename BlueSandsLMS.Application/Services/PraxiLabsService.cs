using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueSandsLMS.Application.Services
{
    public sealed class PraxiLabsService : IPraxiLabsService
    {
        private const string TokenCacheKey = "praxilabs_access_token";
        private const int TokenCacheTtlSeconds = 3540;

        private readonly IHttpClientFactory _httpFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PraxiLabsService> _logger;
        private readonly string _baseUrl;
        private readonly string _credentials;

        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public PraxiLabsService(
            IHttpClientFactory httpFactory,
            IMemoryCache cache,
            IConfiguration config,
            ILogger<PraxiLabsService> logger)
        {
            _httpFactory = httpFactory;
            _cache = cache;
            _logger = logger;

            var section = config.GetSection("PraxiLabs");
            _baseUrl = section["BaseUrl"]?.TrimEnd('/') ?? "https://praxilabs-lms.com";
            var key = section["ApiKey"] ?? string.Empty;
            var secret = section["ApiSecret"] ?? string.Empty;
            _credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{key}:{secret}"));
        }

        public async Task<List<PraxiLabsBranch>> GetBranchesAsync(CancellationToken ct = default)
        {
            var token = await GetTokenAsync(ct);
            using var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{_baseUrl}/api/external/branches", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);

            var wrapper = JsonSerializer.Deserialize<PraxiLabsBranchesRaw>(json, _json);
            var sciences = wrapper?.Sciences ?? new();

            return sciences
                .Where(s => s.Active && !s.Deleted)
                .Select(s => new PraxiLabsBranch
                {
                    Id = s.Id,
                    Name = s.EnglishTitle,
                    ArabicName = s.ArabicTitle,
                    LogoUrl = s.Logo,
                    ExperimentCount = s.NumOfExperiments
                })
                .ToList();
        }

        public async Task<PraxiLabsExperimentsPage> GetExperimentsAsync(
            string? scienceId, string? search, int page, CancellationToken ct = default)
        {
            var token = await GetTokenAsync(ct);
            using var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(scienceId)) qs.Add($"scienceId={Uri.EscapeDataString(scienceId)}");
            if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
            qs.Add($"page={page}");
            var query = "?" + string.Join("&", qs);

            var response = await client.GetAsync($"{_baseUrl}/api/external/experiments{query}", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);

            var wrapper = JsonSerializer.Deserialize<PraxiLabsExperimentsRaw>(json, _json);
            var items = wrapper?.Experiments ?? new();

            return new PraxiLabsExperimentsPage
            {
                Items = items
                    .Where(e => e.Active && !e.Deleted)
                    .Select(MapExperiment)
                    .ToList(),
                Total = wrapper?.Count ?? items.Count,
                Page = page
            };
        }

        public async Task<string> GetCatalogUrlAsync(string userId, CancellationToken ct = default)
        {
            var token = await GetTokenAsync(ct);
            using var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"{_baseUrl}/api/external/catalog?user_id={Uri.EscapeDataString(userId)}";
            var response = await client.GetAsync(url, ct);

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("url", out var urlProp))
                return urlProp.GetString() ?? string.Empty;

            var message = doc.RootElement.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString() ?? "PraxiLabs catalog error"
                : "PraxiLabs did not return a catalog URL";

            throw new InvalidOperationException(message);
        }

        private async Task<string> GetTokenAsync(CancellationToken ct)
        {
            if (_cache.TryGetValue(TokenCacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
                return cached;

            _logger.LogInformation("PraxiLabs: fetching new access token");
            using var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", _credentials);

            var response = await client.GetAsync($"{_baseUrl}/api/external/oauth/token", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("token", out var tokenProp))
                throw new InvalidOperationException("PraxiLabs token response missing 'token' field");

            var token = tokenProp.GetString()
                        ?? throw new InvalidOperationException("PraxiLabs token is null");

            _cache.Set(TokenCacheKey, token, TimeSpan.FromSeconds(TokenCacheTtlSeconds));
            return token;
        }

        private static PraxiLabsExperiment MapExperiment(PraxiLabsExperimentRaw r) =>
            new()
            {
                Id = r.Id,
                Title = r.EnglishTitle ?? r.ArabicTitle ?? string.Empty,
                ArabicTitle = r.ArabicTitle,
                Category = r.Category?.EnglishTitle,
                Description = r.EnglishDescription,
                ThumbnailUrl = r.Logo
            };

        private sealed class PraxiLabsBranchesRaw
        {
            [JsonPropertyName("count")] public int Count { get; set; }
            [JsonPropertyName("sciences")] public List<PraxiLabsBranchRaw> Sciences { get; set; } = new();
        }

        private sealed class PraxiLabsBranchRaw
        {
            [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
            [JsonPropertyName("englishTitle")] public string EnglishTitle { get; set; } = string.Empty;
            [JsonPropertyName("arabicTitle")] public string? ArabicTitle { get; set; }
            [JsonPropertyName("logo")] public string? Logo { get; set; }
            [JsonPropertyName("numOfExperiments")] public int NumOfExperiments { get; set; }
            [JsonPropertyName("active")] public bool Active { get; set; }
            [JsonPropertyName("deleted")] public bool Deleted { get; set; }
        }

        private sealed class PraxiLabsExperimentsRaw
        {
            [JsonPropertyName("count")] public int Count { get; set; }
            [JsonPropertyName("experiments")] public List<PraxiLabsExperimentRaw> Experiments { get; set; } = new();
        }

        private sealed class PraxiLabsExperimentRaw
        {
            [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
            [JsonPropertyName("englishTitle")] public string? EnglishTitle { get; set; }
            [JsonPropertyName("arabicTitle")] public string? ArabicTitle { get; set; }
            [JsonPropertyName("englishDescription")] public string? EnglishDescription { get; set; }
            [JsonPropertyName("arabicDescription")] public string? ArabicDescription { get; set; }
            [JsonPropertyName("logo")] public string? Logo { get; set; }
            [JsonPropertyName("category")] public PraxiLabsCategoryRaw? Category { get; set; }
            [JsonPropertyName("subCategory")] public PraxiLabsCategoryRaw? SubCategory { get; set; }
            [JsonPropertyName("categoryId")] public string? CategoryId { get; set; }
            [JsonPropertyName("scienceId")] public string? ScienceId { get; set; }
            [JsonPropertyName("active")] public bool Active { get; set; }
            [JsonPropertyName("deleted")] public bool Deleted { get; set; }
        }

        private sealed class PraxiLabsCategoryRaw
        {
            [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
            [JsonPropertyName("englishTitle")] public string? EnglishTitle { get; set; }
            [JsonPropertyName("arabicTitle")] public string? ArabicTitle { get; set; }
        }
    }
}
