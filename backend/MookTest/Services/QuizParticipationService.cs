using Microsoft.EntityFrameworkCore;
using MookTest.Data;
using System.Linq.Expressions;
using MookTest.DTO;
using MookTest.Enums;
using MookTest.Model;
using MookTest.Services.Interfaces;

namespace MookTest.Services;
public class QuizParticipationService(AppDbContext db) : IQuizParticipationService
{
    // Keep the catalog, preview and start action in agreement about which quizzes can be taken.
    private static readonly Expression<Func<Quiz, bool>> ReadyQuiz = quiz =>
        quiz.Questions.Any() && quiz.Questions.All(question =>
            question.QuestionType != QuestionType.MultipleChoice &&
            question.QuestionType != QuestionType.SingleChoice &&
            question.QuestionType != QuestionType.TrueFalse ||
            question.Answers.Count() >= 2 && question.Answers.Count(a => a.IsCorrect) >= 1 &&
            (question.QuestionType == QuestionType.MultipleChoice ||
             question.Answers.Count(a => a.IsCorrect) == 1) &&
            (question.QuestionType != QuestionType.TrueFalse || question.Answers.Count() == 2));
    private static readonly Func<Quiz, bool> IsReadyQuiz = ReadyQuiz.Compile();

    public Task<List<QuizDto>> GetAvailableQuizzesAsync(int userId) => db.Quizzes.AsNoTracking()
        .Where(ReadyQuiz).Where(q => !db.QuizSubmissions.Any(s => s.QuizId == q.QuizId && s.UserId == userId))
        .OrderByDescending(q => q.QuizId).Select(q => new QuizDto {
            QuizId = q.QuizId, Title = q.Title, Description = q.Description, Duration = q.Duration,
            ShowAnswersAfterSubmit = q.ShowAnswersAfterSubmit
        }).ToListAsync();

    public Task<QuizDto?> GetQuizDetailsAsync(int id, int userId) => db.Quizzes.AsNoTracking()
        .Where(ReadyQuiz).Where(q => q.QuizId == id &&
            !db.QuizSubmissions.Any(s => s.QuizId == q.QuizId && s.UserId == userId))
        .Select(q => new QuizDto {
            QuizId = q.QuizId, Title = q.Title, Description = q.Description, Duration = q.Duration,
            ShowAnswersAfterSubmit = q.ShowAnswersAfterSubmit
        }).FirstOrDefaultAsync();

    public async Task<StartedQuizDto?> StartQuizAsync(int id, int userId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var quiz = await db.Quizzes.AsNoTracking().Include(q => q.Questions)
            .ThenInclude(q => q.Answers).SingleOrDefaultAsync(q => q.QuizId == id);
        if (quiz is null) return null;
        if (await db.QuizSubmissions.AnyAsync(s => s.QuizId == id && s.UserId == userId))
            throw new BusinessRuleException("Bạn đã nộp bài này. Mỗi học viên chỉ được nộp một lần cho mỗi đề.");
        if (!IsReadyQuiz(quiz))
            throw new BusinessRuleException("Đề chưa có câu hỏi hoặc đáp án hợp lệ. Vui lòng liên hệ Trainer.");
        var now = DateTime.UtcNow;
        var attempt = await db.QuizAttempts.Where(a => a.QuizId == id && a.UserId == userId &&
            a.SubmittedAt == null && a.ExpiresAt > now).OrderByDescending(a => a.StartedAt).FirstOrDefaultAsync();
        if (attempt is null)
        {
            attempt = new QuizAttempt { QuizId = id, UserId = userId, StartedAt = now,
                ExpiresAt = now.AddMinutes(quiz.Duration) };
            db.QuizAttempts.Add(attempt);
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();
        return new StartedQuizDto(quiz.QuizId, quiz.Title, quiz.Description, quiz.Duration,
            attempt.Id, Utc(attempt.StartedAt), Utc(attempt.ExpiresAt),
            quiz.Questions.OrderBy(q => q.QuestionId).Select(q => new TraineeQuestionDto(
                q.QuestionId, q.Content, q.QuestionType, q.Answers.OrderBy(a => a.AnswerId)
                .Select(a => new ChoiceDto(a.AnswerId, a.Content)).ToList())).ToList());
    }

    public async Task<SubmissionDto?> SubmitQuizAsync(int quizId, int userId, SubmitQuizDto dto)
    {
        // The conditional update below claims this attempt exactly once, even for concurrent requests.
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var attempt = await db.QuizAttempts.AsNoTracking().SingleOrDefaultAsync(a =>
            a.Id == dto.AttemptId && a.QuizId == quizId && a.UserId == userId);
        if (attempt is null) return null;
        var now = DateTime.UtcNow;
        if (attempt.SubmittedAt != null) throw new BusinessRuleException("Bài này đã được nộp.");
        if (attempt.ExpiresAt <= now) throw new BusinessRuleException("Đã hết thời gian làm bài.");
        if (await db.QuizSubmissions.AnyAsync(s => s.QuizId == quizId && s.UserId == userId))
            throw new BusinessRuleException("Bạn đã nộp bài này. Mỗi học viên chỉ được nộp một lần cho mỗi đề.");
        var quiz = await db.Quizzes.Include(q => q.Questions).ThenInclude(q => q.Answers)
            .SingleAsync(q => q.QuizId == quizId);
        if (dto.Answers.Count != quiz.Questions.Count ||
            dto.Answers.Select(a => a.QuestionId).Distinct().Count() != quiz.Questions.Count)
            throw new ArgumentException("Hãy trả lời mỗi câu hỏi đúng một lần.");
        var submission = new QuizSubmission { QuizId = quizId, UserId = userId,
            AttemptId = attempt.Id, SubmittedAt = now };
        foreach (var response in dto.Answers)
        {
            var question = quiz.Questions.SingleOrDefault(q => q.QuestionId == response.QuestionId)
                ?? throw new ArgumentException("Câu hỏi không thuộc đề này.");
            var ids = response.AnswerIds;
            if (ids is null) throw new ArgumentException("Danh sách đáp án không hợp lệ.");
            if (IsChoice(question.QuestionType))
            {
                if (ids.Count == 0 || ids.Distinct().Count() != ids.Count ||
                    (question.QuestionType != QuestionType.MultipleChoice && ids.Count != 1) ||
                    ids.Any(id => !question.Answers.Any(a => a.AnswerId == id)) ||
                    !string.IsNullOrWhiteSpace(response.ResponseText))
                    throw new ArgumentException($"Đáp án cho câu {question.QuestionId} không hợp lệ.");
                foreach (var id in ids)
                    submission.SubmissionAnswers.Add(new SubmissionAnswer { QuestionId = question.QuestionId, AnswerId = id });
            }
            else
            {
                if (ids.Count != 0 || string.IsNullOrWhiteSpace(response.ResponseText) || response.ResponseText.Length > 10000)
                    throw new ArgumentException($"Hãy nhập câu trả lời (tối đa 10000 ký tự) cho câu {question.QuestionId}.");
                submission.SubmissionAnswers.Add(new SubmissionAnswer { QuestionId = question.QuestionId,
                    ResponseText = response.ResponseText.Trim() });
            }
        }
        var claimed = await db.QuizAttempts.Where(a => a.Id == attempt.Id &&
            a.SubmittedAt == null && a.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.SubmittedAt, now));
        if (claimed != 1) throw new BusinessRuleException("Bài đã nộp hoặc đã hết hạn.");
        db.QuizSubmissions.Add(submission);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (await GetSubmissionsAsync(userId, quizId)).Single(s => s.QuizSubmissionId == submission.QuizSubmissionId);
    }

    public async Task<List<SubmissionDto>> GetSubmissionsAsync(int? userId, int? quizId = null)
    {
        var query = db.QuizSubmissions.AsNoTracking();
        if (userId.HasValue) query = query.Where(s => s.UserId == userId);
        if (quizId.HasValue) query = query.Where(s => s.QuizId == quizId);
        var rows = await query.Include(s => s.Quiz).ThenInclude(q => q!.Questions).ThenInclude(q => q.Answers)
            .Include(s => s.User)
            .Include(s => s.SubmissionAnswers).ThenInclude(a => a.Question)
            .Include(s => s.SubmissionAnswers).ThenInclude(a => a.Answer)
            .AsSplitQuery().OrderByDescending(s => s.SubmittedAt).ToListAsync();
        return rows.Select(s =>
        {
            var questions = s.Quiz!.Questions.ToDictionary(q => q.QuestionId);
            var choiceQuestions = questions.Values.Where(q => IsChoice(q.QuestionType)).ToList();
            var correct = choiceQuestions.Count(q =>
            {
                var selected = s.SubmissionAnswers.Where(a => a.QuestionId == q.QuestionId && a.AnswerId.HasValue)
                    .Select(a => a.AnswerId!.Value).ToHashSet();
                return selected.SetEquals(q.Answers.Where(a => a.IsCorrect).Select(a => a.AnswerId));
            });
            var reveal = !userId.HasValue || s.Quiz.ShowAnswersAfterSubmit;
            var answers = s.SubmissionAnswers.GroupBy(a => a.QuestionId).Select(g =>
            {
                var question = questions[g.Key];
                return new SubmittedResponseDto(g.Key, question.Content,
                    g.Where(a => a.Answer != null).Select(a => a.Answer!.Content).ToList(),
                    g.FirstOrDefault(a => a.ResponseText != null)?.ResponseText,
                    reveal && IsChoice(question.QuestionType)
                        ? question.Answers.Where(a => a.IsCorrect).Select(a => a.Content).ToList() : null);
            }).ToList();
            return new SubmissionDto(s.QuizSubmissionId, s.QuizId, s.Quiz.Title,
                s.User?.DisplayName, Utc(s.SubmittedAt), answers, correct,
                choiceQuestions.Count, questions.Count - choiceQuestions.Count, reveal);
        }).ToList();
    }

    private static bool IsChoice(QuestionType type) => type is QuestionType.MultipleChoice or
        QuestionType.SingleChoice or QuestionType.TrueFalse;
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}



