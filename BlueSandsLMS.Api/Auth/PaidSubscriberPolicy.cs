using System.Security.Claims;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Auth
{
    public class PaidSubscriberRequirement : IAuthorizationRequirement {}

    public class PaidSubscriberHandler : AuthorizationHandler<PaidSubscriberRequirement>
    {
        private readonly BlueSandsLMSDbContext _db;
        public PaidSubscriberHandler(BlueSandsLMSDbContext db) => _db = db;

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PaidSubscriberRequirement requirement)
        {
            var sub = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(sub, out var userId)) return;

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return;

            var now = DateTime.UtcNow;
            var active = await _db.Subscriptions.AsNoTracking()
                .AnyAsync(s => s.SchoolId == user.SchoolId && s.Active && s.StartsAt <= now && s.EndsAt >= now);

            if (active) context.Succeed(requirement);
        }
    }
}
