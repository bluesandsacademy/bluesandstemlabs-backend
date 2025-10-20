using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BlueSandsLMS.Infrastructure
{
    // Used by `dotnet ef` at design-time so migrations always know how to create the DbContext.
    public sealed class BlueSandsLMSDbContextFactory : IDesignTimeDbContextFactory<BlueSandsLMSDbContext>
    {
        public BlueSandsLMSDbContext CreateDbContext(string[] args)
        {
            // Try to load connection string from the API appsettings (common in layered solutions)
            var apiPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "BlueSandsLMS.Api"));
            var config = new ConfigurationBuilder()
                .SetBasePath(apiPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var conn =
                config.GetConnectionString("DefaultConnection") ??
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
                // Local fallback so dev boxes can still generate migrations
                "Server=(localdb)\\MSSQLLocalDB;Database=BlueSandsLMS;Trusted_Connection=True;MultipleActiveResultSets=true";

            var options = new DbContextOptionsBuilder<BlueSandsLMSDbContext>()
                .UseSqlServer(conn)
                .Options;

            return new BlueSandsLMSDbContext(options);
        }
    }
}
