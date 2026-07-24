using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddIlsPhase2CoreStudentFlow : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PollOptionsJson",
                table: "InteractiveLearningSpaces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentIlsSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IlsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSimulationStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentIlsSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentIlsSessions_InteractiveLearningSpaces_IlsId",
                        column: x => x.IlsId,
                        principalTable: "InteractiveLearningSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentIlsSessions_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionHypotheses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    InputMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionHypotheses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionHypotheses_StudentIlsSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StudentIlsSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionPolls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionIndex = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionPolls_StudentIlsSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StudentIlsSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionTrials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariablesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObservationText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTrials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTrials_StudentIlsSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StudentIlsSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionHypotheses_SessionId",
                table: "SessionHypotheses",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionPolls_SessionId",
                table: "SessionPolls",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionTrials_SessionId_CreatedAt",
                table: "SessionTrials",
                columns: new[] { "SessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentIlsSessions_IlsId_CurrentStep",
                table: "StudentIlsSessions",
                columns: new[] { "IlsId", "CurrentStep" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentIlsSessions_StudentId_IlsId",
                table: "StudentIlsSessions",
                columns: new[] { "StudentId", "IlsId" },
                unique: true);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionHypotheses");

            migrationBuilder.DropTable(
                name: "SessionPolls");

            migrationBuilder.DropTable(
                name: "SessionTrials");

            migrationBuilder.DropTable(
                name: "StudentIlsSessions");

            migrationBuilder.DropColumn(
                name: "PollOptionsJson",
                table: "InteractiveLearningSpaces");
        }
    }
}
