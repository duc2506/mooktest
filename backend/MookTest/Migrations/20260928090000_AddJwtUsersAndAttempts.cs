using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace MookTest.Migrations;

public partial class AddJwtUsersAndAttempts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(name: "Tilte", table: "Quizzes", newName: "Title");
        migrationBuilder.AlterColumn<int>(name: "AnswerId", table: "SubmissionAnswers",
            type: "int", nullable: true, oldClrType: typeof(int), oldType: "int");
        migrationBuilder.CreateTable(name: "Users", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
            NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
            PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
            Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Users", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_Users_NormalizedEmail", table: "Users",
            column: "NormalizedEmail", unique: true);
        migrationBuilder.CreateTable(name: "QuizAttempts", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            QuizId = table.Column<int>(type: "int", nullable: false),
            UserId = table.Column<int>(type: "int", nullable: false),
            StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_QuizAttempts", x => x.Id);
            table.ForeignKey("FK_QuizAttempts_Quizzes_QuizId", x => x.QuizId, "Quizzes", "QuizId",
                onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_QuizAttempts_Users_UserId", x => x.UserId, "Users", "Id",
                onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex(name: "IX_QuizAttempts_QuizId", table: "QuizAttempts", column: "QuizId");
        migrationBuilder.CreateIndex(name: "IX_QuizAttempts_UserId", table: "QuizAttempts", column: "UserId");
        // Nullable references preserve submissions created before accounts existed.
        migrationBuilder.AddColumn<int>(name: "UserId", table: "QuizSubmissions", type: "int", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "AttemptId", table: "QuizSubmissions", type: "uniqueidentifier", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_QuizSubmissions_UserId", table: "QuizSubmissions", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_QuizSubmissions_AttemptId", table: "QuizSubmissions",
            column: "AttemptId", unique: true, filter: "[AttemptId] IS NOT NULL");
        migrationBuilder.AddForeignKey(name: "FK_QuizSubmissions_Users_UserId", table: "QuizSubmissions",
            column: "UserId", principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_QuizSubmissions_QuizAttempts_AttemptId", table: "QuizSubmissions",
            column: "AttemptId", principalTable: "QuizAttempts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Never silently discard written responses when reverting the nullable AnswerId fix.
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [SubmissionAnswers] WHERE [AnswerId] IS NULL) THROW 50001, 'Cannot roll back while written responses exist.', 1;");
        migrationBuilder.DropForeignKey("FK_QuizSubmissions_Users_UserId", "QuizSubmissions");
        migrationBuilder.DropForeignKey("FK_QuizSubmissions_QuizAttempts_AttemptId", "QuizSubmissions");
        migrationBuilder.DropIndex("IX_QuizSubmissions_UserId", "QuizSubmissions");
        migrationBuilder.DropIndex("IX_QuizSubmissions_AttemptId", "QuizSubmissions");
        migrationBuilder.DropColumn("UserId", "QuizSubmissions");
        migrationBuilder.DropColumn("AttemptId", "QuizSubmissions");
        migrationBuilder.DropTable("QuizAttempts");
        migrationBuilder.DropTable("Users");
        migrationBuilder.AlterColumn<int>(name: "AnswerId", table: "SubmissionAnswers",
            type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        migrationBuilder.RenameColumn(name: "Title", table: "Quizzes", newName: "Tilte");
    }
}

