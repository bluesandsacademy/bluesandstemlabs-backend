using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddSchoolNameAndInquiries : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndividualInquiries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    School = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    IsContacted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndividualInquiries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolInquiries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StudentCount = table.Column<int>(type: "int", nullable: false),
                    TeacherCount = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    IsContacted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolInquiries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndividualInquiries_DateCreated",
                table: "IndividualInquiries",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_IndividualInquiries_Email",
                table: "IndividualInquiries",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_IndividualInquiries_IsContacted",
                table: "IndividualInquiries",
                column: "IsContacted");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolInquiries_DateCreated",
                table: "SchoolInquiries",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolInquiries_Email",
                table: "SchoolInquiries",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolInquiries_IsContacted",
                table: "SchoolInquiries",
                column: "IsContacted");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndividualInquiries");

            migrationBuilder.DropTable(
                name: "SchoolInquiries");
        }
    }
}
