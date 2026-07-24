using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddPhase4HardeningIndexes : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StudentIlsSessions_IlsId",
                table: "StudentIlsSessions",
                column: "IlsId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentIlsSessions_StudentId",
                table: "StudentIlsSessions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractiveLearningSpaces_Status",
                table: "InteractiveLearningSpaces",
                column: "Status");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentIlsSessions_IlsId",
                table: "StudentIlsSessions");

            migrationBuilder.DropIndex(
                name: "IX_StudentIlsSessions_StudentId",
                table: "StudentIlsSessions");

            migrationBuilder.DropIndex(
                name: "IX_InteractiveLearningSpaces_Status",
                table: "InteractiveLearningSpaces");
        }
    }
}
