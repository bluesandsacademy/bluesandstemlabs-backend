using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class AddSchoolCurrency : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Schools",
                type: "nvarchar(10)",
                nullable: false,
                defaultValue: "NGN");


            migrationBuilder.Sql("UPDATE Schools SET Currency = 'NGN' WHERE Currency = '' OR Currency IS NULL");


            migrationBuilder.Sql("UPDATE Schools SET Currency = 'UGX' WHERE Id = 'b2222222-0001-4f8c-87b5-da1a1ab50002'");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Schools");
        }
    }
}
