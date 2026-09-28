using System.ComponentModel.DataAnnotations;

namespace MookTest.DTO
{
    public class UpdateAnswerDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}
