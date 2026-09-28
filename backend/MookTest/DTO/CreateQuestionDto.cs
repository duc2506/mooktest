using MookTest.Enums;
using System.ComponentModel.DataAnnotations;

namespace MookTest.DTO
{
    public class CreateQuestionDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(QuestionType))]
        public QuestionType QuestionType { get; set; }
    }
}

