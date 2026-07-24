using System.Reflection;
using System.Text.RegularExpressions;
using BlueSandsLMS.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BlueSandsLMS.Api.OpenApi
{
    public sealed class FrontendOpenApiOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            EnsureOperationId(operation, context);

            var auth = ResolveAuthorization(context);
            if (auth.RequiresAuthentication)
            {
                ApplyBearerSecurity(operation);
                AddAuthMetadata(operation, auth);
                AddErrorResponse(operation, context, "401", "Authentication required.");
                AddErrorResponse(operation, context, "403", "Authenticated user is not allowed to access this resource.");
            }

            AddCommonErrorResponses(operation, context);
        }

        private static void EnsureOperationId(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!string.IsNullOrWhiteSpace(operation.OperationId))
            {
                return;
            }

            var method = (context.ApiDescription.HttpMethod ?? "op").ToLowerInvariant();
            var relativePath = context.ApiDescription.RelativePath?.Split('?')[0] ?? context.MethodInfo.Name;

            var tokens = relativePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizePathToken)
                .Where(token => token.Length > 0);

            var raw = string.Join("_", new[] { method }.Concat(tokens));
            operation.OperationId = ToCamelCase(raw);
        }

        private static void ApplyBearerSecurity(OpenApiOperation operation)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();

            var alreadyDefined = operation.Security.Any(requirement =>
                requirement.Keys.Any(key => string.Equals(key.Reference?.Id, "Bearer", StringComparison.OrdinalIgnoreCase)));

            if (alreadyDefined)
            {
                return;
            }

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            });
        }

        private static void AddAuthMetadata(OpenApiOperation operation, AuthorizationMetadata auth)
        {
            if (auth.Roles.Count > 0)
            {
                var roles = new OpenApiArray();
                foreach (var role in auth.Roles)
                {
                    roles.Add(new OpenApiString(role));
                }

                operation.Extensions["x-required-roles"] = roles;
            }

            if (auth.Policies.Count > 0)
            {
                var policies = new OpenApiArray();
                foreach (var policy in auth.Policies)
                {
                    policies.Add(new OpenApiString(policy));
                }

                operation.Extensions["x-auth-policies"] = policies;
            }

            var summary = auth.Roles.Count > 0
                ? $"Authorization: Bearer token required. Roles: {string.Join(", ", auth.Roles)}."
                : "Authorization: Bearer token required.";

            if (auth.Policies.Count > 0)
            {
                summary = $"{summary} Policies: {string.Join(", ", auth.Policies)}.";
            }

            operation.Description = string.IsNullOrWhiteSpace(operation.Description)
                ? summary
                : $"{operation.Description}{Environment.NewLine}{Environment.NewLine}{summary}";
        }

        private static void AddCommonErrorResponses(OpenApiOperation operation, OperationFilterContext context)
        {
            var method = (context.ApiDescription.HttpMethod ?? string.Empty).ToUpperInvariant();
            var hasRouteParams = context.ApiDescription.ParameterDescriptions.Any(parameter =>
                string.Equals(parameter.Source?.Id, "Path", StringComparison.OrdinalIgnoreCase));

            AddErrorResponse(operation, context, "400", "Validation failed.");

            if (hasRouteParams)
            {
                AddErrorResponse(operation, context, "404", "Requested resource was not found.");
            }

            if (method is "POST" or "PUT" or "PATCH" or "DELETE")
            {
                AddErrorResponse(operation, context, "409", "Request conflicts with current state.");
                AddErrorResponse(operation, context, "422", "Request violates a business rule.");
            }

            if (method == "POST")
            {
                AddErrorResponse(operation, context, "429", "Too many requests. Retry later.");
            }

            AddErrorResponse(operation, context, "500", "Unexpected server error.");
        }

        private static void AddErrorResponse(
            OpenApiOperation operation,
            OperationFilterContext context,
            string statusCode,
            string description)
        {
            var errorSchema = context.SchemaGenerator.GenerateSchema(typeof(ApiErrorResponse), context.SchemaRepository);

            if (!operation.Responses.TryGetValue(statusCode, out var response))
            {
                operation.Responses[statusCode] = new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new() { Schema = errorSchema }
                    }
                };
                return;
            }

            if (string.IsNullOrWhiteSpace(response.Description))
            {
                response.Description = description;
            }

            response.Content ??= new Dictionary<string, OpenApiMediaType>();
            if (!response.Content.ContainsKey("application/json"))
            {
                response.Content["application/json"] = new OpenApiMediaType { Schema = errorSchema };
            }
        }

        private static AuthorizationMetadata ResolveAuthorization(OperationFilterContext context)
        {
            var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata ?? Array.Empty<object>();
            var authorizeData = new List<IAuthorizeData>();

            var allowAnonymous = metadata.OfType<IAllowAnonymous>().Any();
            authorizeData.AddRange(metadata.OfType<IAuthorizeData>());

            var methodAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
            allowAnonymous |= methodAttributes.OfType<IAllowAnonymous>().Any();
            authorizeData.AddRange(methodAttributes.OfType<IAuthorizeData>());

            var declaringType = context.MethodInfo.DeclaringType;
            if (declaringType is not null)
            {
                var declaringAttributes = declaringType.GetCustomAttributes(inherit: true);
                allowAnonymous |= declaringAttributes.OfType<IAllowAnonymous>().Any();
                authorizeData.AddRange(declaringAttributes.OfType<IAuthorizeData>());
            }

            var requiresAuthentication = !allowAnonymous && authorizeData.Count > 0;
            var roles = ParseCsvValues(authorizeData.Select(data => data.Roles));
            var policies = ParseCsvValues(authorizeData.Select(data => data.Policy));

            return new AuthorizationMetadata(requiresAuthentication, roles, policies);
        }

        private static IReadOnlyList<string> ParseCsvValues(IEnumerable<string?> values)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        set.Add(part);
                    }
                }
            }

            return set.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static string NormalizePathToken(string token)
        {
            var cleanedToken = token.Trim();
            if (cleanedToken.StartsWith('{') && cleanedToken.EndsWith('}') && cleanedToken.Length > 2)
            {
                cleanedToken = $"by_{cleanedToken[1..^1]}";
            }

            cleanedToken = Regex.Replace(cleanedToken, @"[^a-zA-Z0-9]+", "_");
            cleanedToken = cleanedToken.Trim('_');
            return cleanedToken.ToLowerInvariant();
        }

        private static string ToCamelCase(string value)
        {
            var parts = value
                .Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
                .ToArray();

            if (parts.Length == 0)
            {
                return "operation";
            }

            var first = parts[0].ToLowerInvariant();
            var remainder = parts.Skip(1)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant());

            return first + string.Concat(remainder);
        }

        private sealed record AuthorizationMetadata(
            bool RequiresAuthentication,
            IReadOnlyList<string> Roles,
            IReadOnlyList<string> Policies);
    }
}
