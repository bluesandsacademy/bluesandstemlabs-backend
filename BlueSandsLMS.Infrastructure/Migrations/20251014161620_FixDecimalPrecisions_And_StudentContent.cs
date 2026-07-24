using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class FixDecimalPrecisions_And_StudentContent : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_UserId_CompletedAt",
                table: "QuizAttempts");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score0to1",
                table: "Submissions",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score0to1",
                table: "QuizAttempts",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "QuizCode",
                table: "QuizAttempts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ExperimentLaunchId",
                table: "QuizAttempts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Passed",
                table: "QuizAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "QuizAttempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "QuizAttempts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "ExperimentLaunches",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "ExperimentLaunches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperimentName",
                table: "ExperimentLaunches",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LastStep",
                table: "ExperimentLaunches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "ExperimentLaunches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "ExperimentLaunches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "BadgeAwards",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "BadgeAwards",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BadgeAwards",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "BadgeAwards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "AuditEvents",
                type: "decimal(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_Subjects_SubjectCode",
                        column: x => x.SubjectCode,
                        principalTable: "Subjects",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMin = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lessons_Subjects_SubjectCode",
                        column: x => x.SubjectCode,
                        principalTable: "Subjects",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonProgresses_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_UserId_QuizCode_CompletedAt",
                table: "QuizAttempts",
                columns: new[] { "UserId", "QuizCode", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentLaunches_UserId_ExperimentName_StartedAt",
                table: "ExperimentLaunches",
                columns: new[] { "UserId", "ExperimentName", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeAwards_UserId_Code",
                table: "BadgeAwards",
                columns: new[] { "UserId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_SubjectCode",
                table: "Certificates",
                column: "SubjectCode");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_UserId_SubjectCode_IssuedAt",
                table: "Certificates",
                columns: new[] { "UserId", "SubjectCode", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgresses_LessonId",
                table: "LessonProgresses",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgresses_UserId_LessonId",
                table: "LessonProgresses",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_SubjectCode_IsActive_SortOrder",
                table: "Lessons",
                columns: new[] { "SubjectCode", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_IsActive",
                table: "Subjects",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SortOrder",
                table: "Subjects",
                column: "SortOrder");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "LessonProgresses");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_UserId_QuizCode_CompletedAt",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ExperimentLaunches_UserId_ExperimentName_StartedAt",
                table: "ExperimentLaunches");

            migrationBuilder.DropIndex(
                name: "IX_BadgeAwards_UserId_Code",
                table: "BadgeAwards");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "ExperimentLaunchId",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "Passed",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "ExperimentLaunches");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "ExperimentLaunches");

            migrationBuilder.DropColumn(
                name: "ExperimentName",
                table: "ExperimentLaunches");

            migrationBuilder.DropColumn(
                name: "LastStep",
                table: "ExperimentLaunches");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ExperimentLaunches");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "ExperimentLaunches");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "BadgeAwards");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "BadgeAwards");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BadgeAwards");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "BadgeAwards");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score0to1",
                table: "Submissions",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score0to1",
                table: "QuizAttempts",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "QuizCode",
                table: "QuizAttempts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "AuditEvents",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_UserId_CompletedAt",
                table: "QuizAttempts",
                columns: new[] { "UserId", "CompletedAt" });
        }
    }
}
