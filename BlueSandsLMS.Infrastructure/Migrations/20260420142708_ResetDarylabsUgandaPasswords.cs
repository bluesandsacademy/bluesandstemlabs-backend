using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSandsLMS.Infrastructure.Migrations
{

    public partial class ResetDarylabsUgandaPasswords : Migration
    {
        private const string DericStudentId     = "65e4f8ee-e4f5-4ec0-9675-a9b04f796548";
        private const string ElizabethStudentId = "5f7175cc-67cb-4372-aa78-64516202d4e8";
        private const string DericTeacherId     = "35ab5ca7-b712-4ec9-97a7-447097af7227";
        private const string ElizabethTeacherId = "ac861ec7-47c4-47d4-9087-8fc5049a8a6e";

        private const string DericStudentHash     = "$2a$11$OcZYS3ZUY6KOZ7ADBwfk1.KvBUqhUS2YYlW.sb43bj5YMChSbF79G";
        private const string ElizabethStudentHash = "$2a$11$lOon6zgkXZiI696oiMlBqua/BZfkPMPXTPF4n9Sw8CBnbQFRFW7IK";
        private const string DericTeacherHash     = "$2a$11$DUExweSRdY6On2u1UNQc.uVEWlVvpKsi3EloP.3gNxzwtvNitrI4y";
        private const string ElizabethTeacherHash = "$2a$11$xNJjcS2ZGSrm3ovT9yNN4ulKMR925l1Ol..ObOGwYa9NjFNP.3/qu";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"UPDATE Users SET PasswordHash = '{DericStudentHash}'     WHERE Id = '{DericStudentId}'");
            migrationBuilder.Sql($"UPDATE Users SET PasswordHash = '{ElizabethStudentHash}' WHERE Id = '{ElizabethStudentId}'");
            migrationBuilder.Sql($"UPDATE Users SET PasswordHash = '{DericTeacherHash}'     WHERE Id = '{DericTeacherId}'");
            migrationBuilder.Sql($"UPDATE Users SET PasswordHash = '{ElizabethTeacherHash}' WHERE Id = '{ElizabethTeacherId}'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
