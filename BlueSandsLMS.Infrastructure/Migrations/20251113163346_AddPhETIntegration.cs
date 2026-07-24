using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddPhETIntegration : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PhETSimulationId",
                table: "ExperimentLaunches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PhETSimulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SimulationUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Topic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LearningGoals = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GradeLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Standards = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhETSimulations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentLaunches_PhETSimulationId",
                table: "ExperimentLaunches",
                column: "PhETSimulationId");

            migrationBuilder.CreateIndex(
                name: "IX_PhETSimulations_GradeLevel",
                table: "PhETSimulations",
                column: "GradeLevel");

            migrationBuilder.CreateIndex(
                name: "IX_PhETSimulations_IsActive",
                table: "PhETSimulations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PhETSimulations_SimulationUrl",
                table: "PhETSimulations",
                column: "SimulationUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhETSimulations_Topic",
                table: "PhETSimulations",
                column: "Topic");

            migrationBuilder.AddForeignKey(
                name: "FK_ExperimentLaunches_PhETSimulations_PhETSimulationId",
                table: "ExperimentLaunches",
                column: "PhETSimulationId",
                principalTable: "PhETSimulations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExperimentLaunches_PhETSimulations_PhETSimulationId",
                table: "ExperimentLaunches");

            migrationBuilder.DropTable(
                name: "PhETSimulations");

            migrationBuilder.DropIndex(
                name: "IX_ExperimentLaunches_PhETSimulationId",
                table: "ExperimentLaunches");

            migrationBuilder.DropColumn(
                name: "PhETSimulationId",
                table: "ExperimentLaunches");
        }
    }
}
