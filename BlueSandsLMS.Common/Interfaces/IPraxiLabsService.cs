namespace BlueSandsLMS.Common.Interfaces
{
    public sealed class PraxiLabsBranch
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ArabicName { get; set; }
        public string? LogoUrl { get; set; }
        public int ExperimentCount { get; set; }
    }

    public sealed class PraxiLabsExperiment
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ArabicTitle { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public sealed class PraxiLabsExperimentsPage
    {
        public List<PraxiLabsExperiment> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
    }

    public interface IPraxiLabsService
    {
        Task<List<PraxiLabsBranch>> GetBranchesAsync(CancellationToken ct = default);

        Task<PraxiLabsExperimentsPage> GetExperimentsAsync(
            string? scienceId, string? search, int page, CancellationToken ct = default);

        Task<string> GetCatalogUrlAsync(string userId, CancellationToken ct = default);
    }
}
