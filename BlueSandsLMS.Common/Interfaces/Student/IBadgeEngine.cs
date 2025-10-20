public interface IBadgeEngine
{
    Task AwardAsync(Guid userId, string eventCode, object? payload, CancellationToken ct);
}