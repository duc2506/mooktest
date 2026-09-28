using System.ComponentModel.DataAnnotations;
namespace MookTest.DTO;
public class SubmitQuizDto
{
    public Guid AttemptId { get; set; }
    [Required, MinLength(1)] public List<SubmitAnswerDto> Answers { get; set; } = [];
}

