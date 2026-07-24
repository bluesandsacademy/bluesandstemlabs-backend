using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BlueSandsLMS.Api.OpenApi
{
    public static class SwaggerGenXmlExtensions
    {
        public static void IncludeXmlCommentsFromAssemblies(this SwaggerGenOptions options, params Assembly[] assemblies)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                if (string.IsNullOrWhiteSpace(assemblyName) || !seen.Add(assemblyName))
                {
                    continue;
                }

                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            }
        }
    }
}
