using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MookTest.Migrations
{
    /// <summary>Stores reusable questions separately from quiz copies.</summary>
    public partial class AddQuestionBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Quizzes_QuizId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_QuizId",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "QuizId", table: "Questions", type: "int", nullable: true,
                oldClrType: typeof(int), oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BankQuestionId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBankItem",
                table: "Questions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuizId_BankQuestionId",
                table: "Questions",
                columns: new[] { "QuizId", "BankQuestionId" },
                unique: true,
                filter: "[QuizId] IS NOT NULL AND [BankQuestionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Quizzes_QuizId", table: "Questions",
                column: "QuizId", principalTable: "Quizzes", principalColumn: "QuizId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Questions] WHERE [QuizId] IS NULL) THROW 50001, 'Cannot roll back while bank questions exist.', 1;");
            migrationBuilder.DropForeignKey(name: "FK_Questions_Quizzes_QuizId", table: "Questions");
            migrationBuilder.DropIndex(
                name: "IX_Questions_QuizId_BankQuestionId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "BankQuestionId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "IsBankItem",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "QuizId", table: "Questions", type: "int", nullable: false,
                oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuizId",
                table: "Questions",
                column: "QuizId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Quizzes_QuizId", table: "Questions",
                column: "QuizId", principalTable: "Quizzes", principalColumn: "QuizId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
