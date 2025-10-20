// Application/Services/Student/BadgeEngine.cs (minimal, idempotent)
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

public sealed class BadgeEngine : IBadgeEngine
{
    private readonly BlueSandsLMSDbContext _db;
    public BadgeEngine(BlueSandsLMSDbContext db) { _db = db; }

    public async Task AwardAsync(Guid userId, string eventCode, object? payload, CancellationToken ct)
    {
        (string code, string name, string? desc) = eventCode switch
        {
            "FIRST_LAUNCH" => ("FIRST_LAUNCH", "First Launch", "Started your first experiment!"),
            "COMPLETED_EXPERIMENT" => ("COMPLETED_EXP", "Experiment Complete", "You finished an experiment."),
            "SCORE_90_PLUS" => ("SCORE_90_PLUS", "Top Scorer", "You scored 90%+ on a quiz."),
            _ => ("GENERIC", "Achievement", null)
        };

        var exists = await _db.BadgeAwards.AnyAsync(b => b.UserId == userId && b.Code == code, ct);
        if (exists) return;

        _db.BadgeAwards.Add(new BadgeAward { Id = Guid.NewGuid(), UserId = userId, Code = code, Name = name, Description = desc });
        await _db.SaveChangesAsync(ct);
    }
}
