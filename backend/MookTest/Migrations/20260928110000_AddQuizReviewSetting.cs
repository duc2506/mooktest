using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MookTest.Data;

namespace MookTest.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260928110000_AddQuizReviewSetting")]
public class AddQuizReviewSetting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ShowAnswersAfterSubmit", table: "Quizzes", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.DropIndex(name: "IX_QuizSubmissions_UserId", table: "QuizSubmissions");
        migrationBuilder.CreateIndex(
            name: "IX_QuizSubmissions_UserId_QuizId", table: "QuizSubmissions", columns: new[] { "UserId", "QuizId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_QuizSubmissions_UserId_QuizId", table: "QuizSubmissions");
        migrationBuilder.CreateIndex(name: "IX_QuizSubmissions_UserId", table: "QuizSubmissions", column: "UserId");
        migrationBuilder.DropColumn(name: "ShowAnswersAfterSubmit", table: "Quizzes");
    }
}
