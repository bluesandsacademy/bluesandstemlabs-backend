using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddPraxiLabsSupport : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SimulationId",
                table: "InteractiveLearningSpaces",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "PraxiLabsExperimentId",
                table: "InteractiveLearningSpaces",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimulationSource",
                table: "InteractiveLearningSpaces",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "phet");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PraxiLabsExperimentId",
                table: "InteractiveLearningSpaces");

            migrationBuilder.DropColumn(
                name: "SimulationSource",
                table: "InteractiveLearningSpaces");

            migrationBuilder.AlterColumn<Guid>(
                name: "SimulationId",
                table: "InteractiveLearningSpaces",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
