using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class SeedDarylabsGlobalAdmin : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DateCreated", "Email", "FullName", "IsActive", "LastLogin", "PasswordHash", "RoleId", "SchoolId", "Phone", "Country", "Gender" },
                values: new object[] {
                    new Guid("d1111111-da12-4f8c-87b5-da1a1ab50001"),
                    new DateTime(2026, 4, 8, 0, 0, 0, 0, DateTimeKind.Utc),
                    "darylabs2@gmail.com",
                    "DaryLabs Admin",
                    true,
                    null,
                    "$2a$11$gwAaIJN6x7xvuNWzC4r9U.KfNYWre1XNEKbjMqzsfdl55967qpCku",
                    new Guid("83b9ce68-4195-4c10-8e08-3dd6af2b0ec9"),
                    null,
                    "",
                    "",
                    ""
                });
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d1111111-da12-4f8c-87b5-da1a1ab50001"));
        }
    }
}
