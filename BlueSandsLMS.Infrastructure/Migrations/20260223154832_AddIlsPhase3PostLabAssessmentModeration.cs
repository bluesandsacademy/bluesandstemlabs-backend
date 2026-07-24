using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddIlsPhase3PostLabAssessmentModeration : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BadgeAwarded",
                table: "StudentIlsSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReflectionSubmitted",
                table: "StudentIlsSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UploadedFileUrl",
                table: "StudentIlsSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentConfigJson",
                table: "InteractiveLearningSpaces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IlsDiscussionMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IlsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Flagged = table.Column<bool>(type: "bit", nullable: false),
                    FlagReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FlaggedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FlaggedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IlsDiscussionMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IlsDiscussionMessages_InteractiveLearningSpaces_IlsId",
                        column: x => x.IlsId,
                        principalTable: "InteractiveLearningSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IlsDiscussionMessages_StudentIlsSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StudentIlsSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IlsDiscussionMessages_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionAssessments_StudentIlsSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StudentIlsSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionReflections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionReflections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionReflections_StudentIlsSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "StudentIlsSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IlsDiscussionMessages_AuthorId",
                table: "IlsDiscussionMessages",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_IlsDiscussionMessages_IlsId_CreatedAt",
                table: "IlsDiscussionMessages",
                columns: new[] { "IlsId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IlsDiscussionMessages_SessionId_CreatedAt",
                table: "IlsDiscussionMessages",
                columns: new[] { "SessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionAssessments_SessionId",
                table: "SessionAssessments",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionReflections_SessionId",
                table: "SessionReflections",
                column: "SessionId",
                unique: true);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IlsDiscussionMessages");

            migrationBuilder.DropTable(
                name: "SessionAssessments");

            migrationBuilder.DropTable(
                name: "SessionReflections");

            migrationBuilder.DropColumn(
                name: "BadgeAwarded",
                table: "StudentIlsSessions");

            migrationBuilder.DropColumn(
                name: "ReflectionSubmitted",
                table: "StudentIlsSessions");

            migrationBuilder.DropColumn(
                name: "UploadedFileUrl",
                table: "StudentIlsSessions");

            migrationBuilder.DropColumn(
                name: "AssessmentConfigJson",
                table: "InteractiveLearningSpaces");
        }
    }
}
