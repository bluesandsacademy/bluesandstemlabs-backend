using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class SeedDarylabsUgandaUsers : Migration
    {
        private const string SchoolId          = "b2222222-0001-4f8c-87b5-da1a1ab50002";
        private const string TeacherRoleId      = "d7c51101-d2a4-40d5-bb0a-bd97898cf847";
        private const string DericStudentId     = "65e4f8ee-e4f5-4ec0-9675-a9b04f796548";
        private const string ElizabethStudentId = "5f7175cc-67cb-4372-aa78-64516202d4e8";
        private const string DericTeacherId     = "35ab5ca7-b712-4ec9-97a7-447097af7227";
        private const string ElizabethTeacherId = "ac861ec7-47c4-47d4-9087-8fc5049a8a6e";


        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.InsertData(
                table: "Schools",
                columns: new[] { "Id", "DateCreated", "IsActive", "Name", "Subdomain" },
                values: new object[]
                {
                    new Guid(SchoolId),
                    new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                    true,
                    "DaryLabs Uganda",
                    "darylabs-uganda"
                });


            migrationBuilder.Sql($@"
                UPDATE Users SET RoleId = '{TeacherRoleId}'
                WHERE Id IN ('{DericTeacherId}', '{ElizabethTeacherId}')");


            migrationBuilder.Sql($@"
                UPDATE Users SET SchoolId = '{SchoolId}'
                WHERE Id IN ('{DericStudentId}', '{ElizabethStudentId}', '{DericTeacherId}', '{ElizabethTeacherId}')");


            migrationBuilder.Sql($@"
                UPDATE Users SET IsEmailVerified = 1, EmailVerifiedAt = GETUTCDATE()
                WHERE Id IN ('{DericStudentId}', '{ElizabethStudentId}', '{DericTeacherId}', '{ElizabethTeacherId}')");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: new Guid(DericStudentId));
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: new Guid(ElizabethStudentId));
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: new Guid(DericTeacherId));
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: new Guid(ElizabethTeacherId));
            migrationBuilder.DeleteData(table: "Schools", keyColumn: "Id", keyValue: new Guid(SchoolId));
        }
    }
}
