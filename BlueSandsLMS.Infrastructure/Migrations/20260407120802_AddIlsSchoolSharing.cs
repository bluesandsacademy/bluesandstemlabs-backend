using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddIlsSchoolSharing : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSharedWithSchool",
                table: "InteractiveLearningSpaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "InteractiveLearningSpaces",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InteractiveLearningSpaces_SchoolId_IsSharedWithSchool",
                table: "InteractiveLearningSpaces",
                columns: new[] { "SchoolId", "IsSharedWithSchool" });

            migrationBuilder.AddForeignKey(
                name: "FK_InteractiveLearningSpaces_Schools_SchoolId",
                table: "InteractiveLearningSpaces",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InteractiveLearningSpaces_Schools_SchoolId",
                table: "InteractiveLearningSpaces");

            migrationBuilder.DropIndex(
                name: "IX_InteractiveLearningSpaces_SchoolId_IsSharedWithSchool",
                table: "InteractiveLearningSpaces");

            migrationBuilder.DropColumn(
                name: "IsSharedWithSchool",
                table: "InteractiveLearningSpaces");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "InteractiveLearningSpaces");
        }
    }
}
