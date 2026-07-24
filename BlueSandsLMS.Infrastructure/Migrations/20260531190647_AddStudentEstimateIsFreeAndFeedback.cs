using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddStudentEstimateIsFreeAndFeedback : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisabledStudentCount",
                table: "Schools",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedScienceStudentCount",
                table: "Schools",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "PhETSimulations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Schools",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111111"),
                columns: new[] { "DisabledStudentCount", "EstimatedScienceStudentCount" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_Category",
                table: "Feedbacks",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_DateCreated",
                table: "Feedbacks",
                column: "DateCreated");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_Status",
                table: "Feedbacks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_UserId",
                table: "Feedbacks",
                column: "UserId");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "DisabledStudentCount",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "EstimatedScienceStudentCount",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "IsFree",
                table: "PhETSimulations");
        }
    }
}
