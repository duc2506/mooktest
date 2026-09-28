using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MookTest.Enums;
namespace MookTest.Model
{
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }
        public int? QuizId { get; set; }
        public bool IsBankItem { get; set; }
        public int? BankQuestionId { get; set; }
        [ForeignKey(nameof(QuizId))]
        public Quiz? Quiz { get; set; }
        [Required]
        public string Content { get; set; } = string.Empty;
        [Required]
        public QuestionType QuestionType { get; set; }
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<SubmissionAnswer> SubmissionsAnswers { get; set; } = new List<SubmissionAnswer>();

    }
}
