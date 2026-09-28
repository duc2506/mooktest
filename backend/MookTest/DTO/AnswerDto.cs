namespace MookTest.DTO
{
    public class AnswerDto
    {
        public int AnswerId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
