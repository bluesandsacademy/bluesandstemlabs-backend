using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddIlsPhase1Foundation : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassMessages_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassMessages_Users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassroomTeachers",
                columns: table => new
                {
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomTeachers", x => new { x.ClassroomId, x.TeacherUserId });
                    table.ForeignKey(
                        name: "FK_ClassroomTeachers_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassroomTeachers_Users_TeacherUserId",
                        column: x => x.TeacherUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InteractiveLearningSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    SimulationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractiveLearningSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InteractiveLearningSpaces_PhETSimulations_SimulationId",
                        column: x => x.SimulationId,
                        principalTable: "PhETSimulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InteractiveLearningSpaces_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassMessageReads",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassMessageReads", x => new { x.MessageId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ClassMessageReads_ClassMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ClassMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassMessageReads_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IlsTags",
                columns: table => new
                {
                    IlsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IlsTags", x => new { x.IlsId, x.TagId });
                    table.ForeignKey(
                        name: "FK_IlsTags_CurriculumTags_TagId",
                        column: x => x.TagId,
                        principalTable: "CurriculumTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IlsTags_InteractiveLearningSpaces_IlsId",
                        column: x => x.IlsId,
                        principalTable: "InteractiveLearningSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassMessageReads_UserId",
                table: "ClassMessageReads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassMessages_ClassroomId",
                table: "ClassMessages",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassMessages_FromUserId",
                table: "ClassMessages",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomTeachers_TeacherUserId",
                table: "ClassroomTeachers",
                column: "TeacherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTags_Label_Subject",
                table: "CurriculumTags",
                columns: new[] { "Label", "Subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IlsTags_TagId",
                table: "IlsTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractiveLearningSpaces_CreatedBy_Status",
                table: "InteractiveLearningSpaces",
                columns: new[] { "CreatedBy", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InteractiveLearningSpaces_SimulationId",
                table: "InteractiveLearningSpaces",
                column: "SimulationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassMessageReads");

            migrationBuilder.DropTable(
                name: "ClassroomTeachers");

            migrationBuilder.DropTable(
                name: "IlsTags");

            migrationBuilder.DropTable(
                name: "ClassMessages");

            migrationBuilder.DropTable(
                name: "CurriculumTags");

            migrationBuilder.DropTable(
                name: "InteractiveLearningSpaces");
        }
    }
}
