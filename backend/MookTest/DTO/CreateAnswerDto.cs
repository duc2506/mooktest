using System.ComponentModel.DataAnnotations;

namespace MookTest.DTO
{
    public class CreateAnswerDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}
