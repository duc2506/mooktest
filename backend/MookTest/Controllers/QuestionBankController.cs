using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MookTest.Data;
using MookTest.DTO;
using MookTest.Enums;
using MookTest.Model;
using MookTest.Services;

namespace MookTest.Controllers;

[ApiController, Route("api/question-bank"), Authorize(Roles = Roles.Trainer)]
public class QuestionBankController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Questions.AsNoTracking()
        .Where(q => q.IsBankItem)
        .OrderByDescending(q => q.QuestionId)
        .Select(q => new QuestionDto
        {
            QuestionId = q.QuestionId,
            Content = q.Content,
            QuestionType = q.QuestionType,
            Answers = q.Answers.OrderBy(a => a.AnswerId).Select(a => new AnswerDto
            {
                AnswerId = a.AnswerId, Content = a.Content, IsCorrect = a.IsCorrect
            }).ToList()
        }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(UpsertBankQuestionDto dto)
    {
        Validate(dto);
        var question = new Question { IsBankItem = true, Content = dto.Content.Trim(),
            QuestionType = dto.QuestionType, Answers = MakeAnswers(dto) };
        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return Ok(ToDto(question));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpsertBankQuestionDto dto)
    {
        Validate(dto);
        var question = await db.Questions.Include(q => q.Answers)
            .SingleOrDefaultAsync(q => q.QuestionId == id && q.IsBankItem);
        if (question is null) return NotFound(new { message = "Không tìm thấy câu hỏi trong ngân hàng." });
        question.Content = dto.Content.Trim();
        question.QuestionType = dto.QuestionType;
        db.Answers.RemoveRange(question.Answers);
        question.Answers = MakeAnswers(dto);
        await db.SaveChangesAsync();
        return Ok(ToDto(question));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var question = await db.Questions.SingleOrDefaultAsync(q => q.QuestionId == id && q.IsBankItem);
        if (question is null) return NotFound(new { message = "Không tìm thấy câu hỏi trong ngân hàng." });
        db.Questions.Remove(question);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("/api/quizzes/{quizId:int}/questions/from-bank")]
    public async Task<IActionResult> Import(int quizId, ImportBankQuestionsDto dto)
    {
        var ids = dto.QuestionIds;
        if (ids is null || ids.Count is 0 or > 50 || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            return BadRequest(new { message = "Chọn từ 1 đến 50 câu hỏi khác nhau." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Quizzes.AnyAsync(q => q.QuizId == quizId))
            return NotFound(new { message = "Không tìm thấy đề kiểm tra." });
        if (await db.QuizAttempts.AnyAsync(a => a.QuizId == quizId) ||
            await db.QuizSubmissions.AnyAsync(s => s.QuizId == quizId))
            throw new BusinessRuleException("Đề đã có lượt làm bài. Hãy tạo đề mới để bảo toàn bài làm cũ.");

        var originals = await db.Questions.AsNoTracking().Include(q => q.Answers)
            .Where(q => q.IsBankItem && ids.Contains(q.QuestionId)).ToListAsync();
        if (originals.Count != ids.Count)
            return NotFound(new { message = "Một số câu hỏi không còn trong ngân hàng." });
        if (await db.Questions.AnyAsync(q => q.QuizId == quizId && q.BankQuestionId != null &&
            ids.Contains(q.BankQuestionId.Value)))
            return Conflict(new { message = "Đề đã có ít nhất một câu hỏi được chọn." });

        foreach (var original in originals)
            db.Questions.Add(new Question
            {
                QuizId = quizId,
                BankQuestionId = original.QuestionId,
                Content = original.Content,
                QuestionType = original.QuestionType,
                Answers = original.Answers.OrderBy(a => a.AnswerId)
                    .Select(a => new Answer { Content = a.Content, IsCorrect = a.IsCorrect }).ToList()
            });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { imported = originals.Count });
    }

    private static List<Answer> MakeAnswers(UpsertBankQuestionDto dto) => dto.Answers
        .Select(a => new Answer { Content = a.Content.Trim(), IsCorrect = a.IsCorrect }).ToList();

    private static QuestionDto ToDto(Question q) => new()
    {
        QuestionId = q.QuestionId, Content = q.Content, QuestionType = q.QuestionType,
        Answers = q.Answers.Select(a => new AnswerDto
        { AnswerId = a.AnswerId, Content = a.Content, IsCorrect = a.IsCorrect }).ToList()
    };

    private static void Validate(UpsertBankQuestionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 10000 ||
            !Enum.IsDefined(dto.QuestionType))
            throw new ArgumentException("Nội dung hoặc loại câu hỏi không hợp lệ.");
        if (dto.Answers is null)
            throw new ArgumentException("Danh sách đáp án không hợp lệ.");
        var choice = dto.QuestionType is QuestionType.MultipleChoice or QuestionType.SingleChoice or QuestionType.TrueFalse;
        if (!choice && dto.Answers.Count != 0)
            throw new ArgumentException("Câu tự luận không sử dụng đáp án lựa chọn.");
        if (!choice) return;
        if (dto.Answers.Count < 2 || dto.Answers.Count > 20 ||
            (dto.QuestionType == QuestionType.TrueFalse && dto.Answers.Count != 2) ||
            dto.Answers.Any(a => a is null || string.IsNullOrWhiteSpace(a.Content) || a.Content.Length > 1000))
            throw new ArgumentException("Câu trắc nghiệm cần 2 đến 20 đáp án hợp lệ.");
        var correct = dto.Answers.Count(a => a.IsCorrect);
        if (correct == 0 || (dto.QuestionType != QuestionType.MultipleChoice && correct != 1))
            throw new ArgumentException("Hãy chọn số đáp án đúng phù hợp với loại câu hỏi.");
    }
}
