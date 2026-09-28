namespace MookTest.DTO;
public class SubmitAnswerDto
{
    public int QuestionId { get; set; }
    public List<int> AnswerIds { get; set; } = [];
    public string? ResponseText { get; set; }
}

