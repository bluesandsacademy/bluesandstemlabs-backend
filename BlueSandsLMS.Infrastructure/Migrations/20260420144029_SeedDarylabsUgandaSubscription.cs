using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class SeedDarylabsUgandaSubscription : Migration
    {
        private const string SchoolId       = "b2222222-0001-4f8c-87b5-da1a1ab50002";
        private const string SubscriptionId = "ee000001-0000-4000-a000-000000000001";


        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[]
                {
                    "Id", "DateCreated", "DateModified", "IsDeleted",
                    "SchoolId", "UserId",
                    "StudentsCovered", "PricePerStudent",
                    "StartsAt", "EndsAt",
                    "Active", "LastPaymentReference"
                },
                values: new object[]
                {
                    new Guid(SubscriptionId),
                    new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                    false,
                    new Guid(SchoolId),
                    null,
                    50,
                    0m,
                    new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2027, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                    true,
                    "DARYLABS-UGANDA-COMPLIMENTARY"
                });


            migrationBuilder.Sql($"UPDATE Schools SET IsActive = 1 WHERE Id = '{SchoolId}'");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Subscriptions", keyColumn: "Id", keyValue: new Guid(SubscriptionId));
        }
    }
}
