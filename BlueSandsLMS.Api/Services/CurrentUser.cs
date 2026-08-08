using System.Security.Claims;

namespace BlueSandsLMS.Api.Services
{
    public interface ICurrentUser
    {
        ClaimsPrincipal Principal { get; }
        bool IsAuthenticated { get; }
        Guid? UserId { get; }
        Guid GetUserId(); // throws InvalidOperationException if no user id
        string? Subject { get; }
        string? Email { get; }
        string? Name { get; }
        string? FullName { get; }
        Guid? SchoolId { get; }
        IEnumerable<string> Roles { get; }
        string? GetClaim(string type);
        bool IsInRole(string role);
    }

    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor;

        public CurrentUser(IHttpContextAccessor accessor)
        {
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        }

        private ClaimsPrincipal? PrincipalInternal => _accessor.HttpContext?.User;

        public ClaimsPrincipal Principal => PrincipalInternal ?? new ClaimsPrincipal();

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public string? GetClaim(string type)
        {
            if (Principal == null) return null;

            // try exact match first (fast)
            var claim = Principal.FindFirst(type);
            if (claim != null) return claim.Value;

            // fallback: case-insensitive search for tokens that use different casing
            var fallback = Principal.Claims.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase));
            return fallback?.Value;
        }

        public string? Subject =>
            GetClaim("sub") ??
            GetClaim(ClaimTypes.NameIdentifier) ??
            GetClaim("nameidentifier") ??
            GetClaim("id");

        public Guid? UserId
        {
            get
            {
                var id = Subject;
                if (string.IsNullOrWhiteSpace(id)) return null;
                return Guid.TryParse(id, out var g) ? g : (Guid?)null;
            }
        }

        public Guid GetUserId()
        {
            var id = UserId;
            if (id == null) throw new InvalidOperationException("Authenticated user id is not available.");
            return id.Value;
        }

        public string? Email =>
            GetClaim(ClaimTypes.Email) ??
            GetClaim("email");

        public string? Name =>
            GetClaim(ClaimTypes.Name) ??
            GetClaim("name");

        public string? FullName =>
            GetClaim("FullName") ??
            GetClaim("fullname") ??
            GetClaim("fullName");

        public Guid? SchoolId
        {
            get
            {
                var s = GetClaim("SchoolId") ?? GetClaim("schoolid");
                if (string.IsNullOrWhiteSpace(s)) return null;
                return Guid.TryParse(s, out var g) ? g : (Guid?)null;
            }
        }

        public IEnumerable<string> Roles
        {
            get
            {
                if (Principal == null) return Enumerable.Empty<string>();

                var roleClaimValues = Enumerable.Empty<string>();

                // common claim types for roles
                var roleClaims = Principal.FindAll(ClaimTypes.Role)
                    .Concat(Principal.FindAll("role"))
                    .Concat(Principal.FindAll("roles"));

                roleClaimValues = roleClaims.Select(c => c.Value ?? string.Empty);

                // handle comma-separated roles in a single claim value
                var split = roleClaimValues
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => v.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                return split;
            }
        }

        public bool IsInRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            return Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }
    }
}