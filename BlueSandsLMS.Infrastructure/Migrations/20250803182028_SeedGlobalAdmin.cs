using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedGlobalAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Schools",
                columns: new[] { "Id", "DateCreated", "IsActive", "Name", "Subdomain" },
                values: new object[] { new Guid("a1111111-1111-1111-1111-111111111111"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Blue Sands Test School", "bluesands-test" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DateCreated", "Email", "FullName", "IsActive", "LastLogin", "PasswordHash", "RoleId", "SchoolId" },
                values: new object[] { new Guid("36a6c8c6-7f46-4043-939e-382dbb42db2b"), new DateTime(2024, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), "ifemicheal2@gmail.com", "Ifedayo Michael", true, null, "$2a$11$cEhjobe.nmtMXJHMXQWhW.a7HJrFSOdBhXdqYi2Oj4BYeTh9LXC2y", new Guid("83b9ce68-4195-4c10-8e08-3dd6af2b0ec9"), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Schools",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("36a6c8c6-7f46-4043-939e-382dbb42db2b"));
        }
    }
}
