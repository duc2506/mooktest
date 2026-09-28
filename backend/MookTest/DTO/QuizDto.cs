namespace MookTest.DTO
{
    public class QuizDto
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int Duration { get; set; }
        public bool ShowAnswersAfterSubmit { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }
}
