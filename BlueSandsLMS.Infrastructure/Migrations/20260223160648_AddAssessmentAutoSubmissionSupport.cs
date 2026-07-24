using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddAssessmentAutoSubmissionSupport : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoSubmitted",
                table: "SessionAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutoSubmitted",
                table: "SessionAssessments");
        }
    }
}
