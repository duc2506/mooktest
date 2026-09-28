using System.ComponentModel.DataAnnotations;
using MookTest.Enums;

namespace MookTest.DTO;

public class BankAnswerInputDto
{
    [Required] public string Content { get; set; } = "";
    public bool IsCorrect { get; set; }
}

public class UpsertBankQuestionDto
{
    [Required] public string Content { get; set; } = "";
    [EnumDataType(typeof(QuestionType))] public QuestionType QuestionType { get; set; }
    public List<BankAnswerInputDto> Answers { get; set; } = [];
}

public class ImportBankQuestionsDto
{
    [Required, MinLength(1)] public List<int> QuestionIds { get; set; } = [];
}
