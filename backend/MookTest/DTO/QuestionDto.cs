using MookTest.Enums;
namespace MookTest.DTO
{
    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public List<AnswerDto> Answers { get; set; } = new();
    }
}
