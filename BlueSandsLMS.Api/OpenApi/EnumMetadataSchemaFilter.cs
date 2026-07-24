using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BlueSandsLMS.Api.OpenApi
{
    public sealed class EnumMetadataSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var enumType = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
            if (!enumType.IsEnum)
            {
                return;
            }

            var names = Enum.GetNames(enumType);
            var values = Enum
                .GetValues(enumType)
                .Cast<object>()
                .Select(Convert.ToInt64)
                .ToArray();

            var enumNames = new OpenApiArray();
            foreach (var name in names)
            {
                enumNames.Add(new OpenApiString(name));
            }

            var enumValues = new OpenApiArray();
            foreach (var value in values)
            {
                enumValues.Add(new OpenApiLong(value));
            }

            schema.Extensions["x-enumNames"] = enumNames;
            schema.Extensions["x-enumValues"] = enumValues;

            var pairs = names.Select((name, index) => $"{name} ({values[index]})");
            var hint = $"Allowed values: {string.Join(", ", pairs)}.";
            schema.Description = string.IsNullOrWhiteSpace(schema.Description)
                ? hint
                : $"{schema.Description} {hint}";
        }
    }
}
