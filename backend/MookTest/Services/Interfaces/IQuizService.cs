using MookTest.DTO;
namespace MookTest.Services.Interfaces
{
    public interface IQuizService
    {
        Task<List<QuizDto>> GetQuizzesAsync();
        Task<QuizDto?> GetQuizByIdAsync(int id);
        Task<QuizDto> CreateQuizAsync(CreateQuizDto dto);

        Task<bool> UpdateQuizAsync(
            int id,
            UpdateQuizDto dto);

        Task<bool> DeleteQuizAsync(int id);

        Task<QuestionDto?> AddQuestionAsync(
            int quizId,
            CreateQuestionDto dto);

        Task<QuestionDto?> UpdateQuestionAsync(
            int quizId,
            int questionId,
            UpdateQuestionDto dto);

        Task<bool> DeleteQuestionAsync(
            int quizId,
            int questionId);

        Task<AnswerDto?> AddAnswerAsync(
            int quizId,
            int questionId,
            CreateAnswerDto dto);

        Task<AnswerDto?> UpdateAnswerAsync(
            int quizId,
            int questionId,
            int answerId,
            UpdateAnswerDto dto);

        Task<bool> DeleteAnswerAsync(
            int quizId,
            int questionId,
            int answerId);
    }
}
