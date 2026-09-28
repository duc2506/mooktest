using MookTest.DTO;
using MookTest.Services.Interfaces;
using MookTest.Model;
using MookTest.Data;
using Microsoft.EntityFrameworkCore;   
namespace MookTest.Services
{
    public class QuizService : IQuizService
    {
        private readonly AppDbContext _context;

        public QuizService(AppDbContext context)
        {
            _context = context;
        }

        private async Task EnsureEditableAsync(int quizId)
        {
            if (await _context.QuizAttempts.AnyAsync(a => a.QuizId == quizId) ||
                await _context.QuizSubmissions.AnyAsync(s => s.QuizId == quizId))
                throw new BusinessRuleException("Đề đã có lượt làm bài. Hãy tạo đề mới để bảo toàn bài làm cũ.");
        }

        private static void ValidateAnswerType(MookTest.Enums.QuestionType type)
        {
            if (type is not (MookTest.Enums.QuestionType.MultipleChoice or MookTest.Enums.QuestionType.SingleChoice or MookTest.Enums.QuestionType.TrueFalse))
                throw new ArgumentException("Câu tự luận không sử dụng đáp án lựa chọn.");
        }

        // UC01
        public async Task<List<QuizDto>> GetQuizzesAsync()
        {
            return await _context.Quizzes
                .AsNoTracking()
                .Select(q => new QuizDto
                {
                    QuizId = q.QuizId,
                    Title = q.Title,
                    Description = q.Description,
                    Duration = q.Duration
                })
                .ToListAsync();
        }

        // UC02
        public async Task<QuizDto?> GetQuizByIdAsync(int id)
        {
            return await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.QuizId == id)
                .Select(q => new QuizDto
                {
                    QuizId = q.QuizId,
                    Title = q.Title,
                    Description = q.Description,
                    Duration = q.Duration,

                    Questions = q.Questions
                        .Select(question => new QuestionDto
                        {
                            QuestionId = question.QuestionId,
                            Content = question.Content,
                            QuestionType = question.QuestionType,

                            Answers = question.Answers
                                .Select(answer => new AnswerDto
                                {
                                    AnswerId = answer.AnswerId,
                                    Content = answer.Content,
                                    IsCorrect = answer.IsCorrect
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        // UC03
        public async Task<QuizDto> CreateQuizAsync(
            CreateQuizDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            Quiz quiz = new()
            {
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                Duration = dto.Duration
            };

            _context.Quizzes.Add(quiz);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new QuizDto
            {
                QuizId = quiz.QuizId,
                Title = quiz.Title,
                Description = quiz.Description,
                Duration = quiz.Duration
            };
        }

        // UC04
        public async Task<bool> UpdateQuizAsync(
            int id,
            UpdateQuizDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            Quiz? quiz = await _context.Quizzes
                .FindAsync(id);

            if (quiz == null)
            {
                return false;
            }

            await EnsureEditableAsync(id);
            quiz.Title = dto.Title.Trim();
            quiz.Description = dto.Description?.Trim();
            quiz.Duration = dto.Duration;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

        // UC05
        public async Task<bool> DeleteQuizAsync(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            Quiz? quiz = await _context.Quizzes
                .FindAsync(id);

            if (quiz == null)
            {
                return false;
            }

            await EnsureEditableAsync(id);
            _context.Quizzes.Remove(quiz);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

        // UC06 - Add Question
        public async Task<QuestionDto?> AddQuestionAsync(
            int quizId,
            CreateQuestionDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            bool exists = await _context.Quizzes
                .AnyAsync(q => q.QuizId == quizId);

            if (!exists)
            {
                return null;
            }

            await EnsureEditableAsync(quizId);
            Question question = new()
            {
                QuizId = quizId,
                Content = dto.Content.Trim(),
                QuestionType = dto.QuestionType
            };

            _context.Questions.Add(question);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new QuestionDto
            {
                QuestionId = question.QuestionId,
                Content = question.Content,
                QuestionType = question.QuestionType
            };
        }

        // UC06 - Update
        public async Task<QuestionDto?> UpdateQuestionAsync(
            int quizId,
            int questionId,
            UpdateQuestionDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            Question? question =
                await _context.Questions
                    .FirstOrDefaultAsync(q =>
                        q.QuestionId == questionId &&
                        q.QuizId == quizId);

            if (question == null)
            {
                return null;
            }

            await EnsureEditableAsync(quizId);
            if (question.QuestionType != dto.QuestionType && await _context.Answers.AnyAsync(a => a.QuestionId == questionId))
                throw new BusinessRuleException("Hãy xóa các đáp án trước khi đổi loại câu hỏi.");
            question.Content = dto.Content.Trim();
            question.QuestionType = dto.QuestionType;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new QuestionDto
            {
                QuestionId = question.QuestionId,
                Content = question.Content,
                QuestionType = question.QuestionType
            };
        }

        // UC06 - Delete
        public async Task<bool> DeleteQuestionAsync(
            int quizId,
            int questionId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            Question? question =
                await _context.Questions
                    .FirstOrDefaultAsync(q =>
                        q.QuestionId == questionId &&
                        q.QuizId == quizId);

            if (question == null)
            {
                return false;
            }

            await EnsureEditableAsync(quizId);
            _context.Questions.Remove(question);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

        // UC07 Add Answer
        public async Task<AnswerDto?> AddAnswerAsync(
            int quizId,
            int questionId,
            CreateAnswerDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            Question? question =
                await _context.Questions
                    .FirstOrDefaultAsync(q =>
                        q.QuestionId == questionId &&
                        q.QuizId == quizId);

            if (question == null)
            {
                return null;
            }

            await EnsureEditableAsync(quizId);
            ValidateAnswerType(question.QuestionType);
            Answer answer = new()
            {
                QuestionId = questionId,
                Content = dto.Content.Trim(),
                IsCorrect = dto.IsCorrect
            };

            _context.Answers.Add(answer);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AnswerDto
            {
                AnswerId = answer.AnswerId,
                Content = answer.Content,
                IsCorrect = answer.IsCorrect
            };
        }

        // UC07 Update Answer
        public async Task<AnswerDto?> UpdateAnswerAsync(
            int quizId,
            int questionId,
            int answerId,
            UpdateAnswerDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            Answer? answer =
                await _context.Answers
                    .Include(a => a.Question)
                    .FirstOrDefaultAsync(a =>
                        a.AnswerId == answerId &&
                        a.QuestionId == questionId &&
                        a.Question!.QuizId == quizId);

            if (answer == null)
            {
                return null;
            }

            await EnsureEditableAsync(quizId);
            ValidateAnswerType(answer.Question!.QuestionType);
            answer.Content = dto.Content.Trim();
            answer.IsCorrect = dto.IsCorrect;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AnswerDto
            {
                AnswerId = answer.AnswerId,
                Content = answer.Content,
                IsCorrect = answer.IsCorrect
            };
        }

        // UC07 Delete Answer
        public async Task<bool> DeleteAnswerAsync(
            int quizId,
            int questionId,
            int answerId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            Answer? answer =
                await _context.Answers
                    .Include(a => a.Question)
                    .FirstOrDefaultAsync(a =>
                        a.AnswerId == answerId &&
                        a.QuestionId == questionId &&
                        a.Question!.QuizId == quizId);

            if (answer == null)
            {
                return false;
            }

            await EnsureEditableAsync(quizId);
            _context.Answers.Remove(answer);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
    }
}



