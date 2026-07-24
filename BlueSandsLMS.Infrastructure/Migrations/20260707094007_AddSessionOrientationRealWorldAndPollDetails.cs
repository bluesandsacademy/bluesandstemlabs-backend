using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddSessionOrientationRealWorldAndPollDetails : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AddColumn<string>(
                name: "AnswersJson",
                table: "SessionPolls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorrectAnswers",
                table: "SessionPolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuizTitle",
                table: "SessionPolls",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "SessionPolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeSpentSeconds",
                table: "SessionPolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestions",
                table: "SessionPolls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionOrientations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EngagementAnswer = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionOrientations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionOrientations_StudentIlsSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StudentIlsSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionRealWorlds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRealWorlds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRealWorlds_StudentIlsSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StudentIlsSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionOrientations_SessionId",
                table: "SessionOrientations",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRealWorlds_SessionId",
                table: "SessionRealWorlds",
                column: "SessionId",
                unique: true);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionOrientations");

            migrationBuilder.DropTable(
                name: "SessionRealWorlds");

            migrationBuilder.DropColumn(
                name: "AnswersJson",
                table: "SessionPolls");

            migrationBuilder.DropColumn(
                name: "CorrectAnswers",
                table: "SessionPolls");

            migrationBuilder.DropColumn(
                name: "QuizTitle",
                table: "SessionPolls");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "SessionPolls");

            migrationBuilder.DropColumn(
                name: "TimeSpentSeconds",
                table: "SessionPolls");

            migrationBuilder.DropColumn(
                name: "TotalQuestions",
                table: "SessionPolls");
        }
    }
}
