using MookTest.Enums;
namespace MookTest.DTO;
public record ChoiceDto(int AnswerId, string Content);
public record TraineeQuestionDto(int QuestionId, string Content, QuestionType QuestionType, List<ChoiceDto> Answers);
public record StartedQuizDto(int QuizId, string Title, string? Description, int Duration,
    Guid AttemptId, DateTime StartedAt, DateTime ExpiresAt, List<TraineeQuestionDto> Questions);
public record SubmissionDto(int QuizSubmissionId, int QuizId, string Title, string? TraineeName,
    DateTime SubmittedAt, List<SubmittedResponseDto> Answers);
public record SubmittedResponseDto(int QuestionId, string Content, List<string> SelectedAnswers, string? ResponseText);

