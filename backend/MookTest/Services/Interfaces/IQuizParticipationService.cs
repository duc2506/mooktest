using MookTest.DTO;
namespace MookTest.Services.Interfaces;
public interface IQuizParticipationService
{
    Task<List<QuizDto>> GetAvailableQuizzesAsync();
    Task<QuizDto?> GetQuizDetailsAsync(int id);
    Task<StartedQuizDto?> StartQuizAsync(int id, int userId);
    Task<SubmissionDto?> SubmitQuizAsync(int quizId, int userId, SubmitQuizDto dto);
    Task<List<SubmissionDto>> GetSubmissionsAsync(int? userId, int? quizId = null);
}

