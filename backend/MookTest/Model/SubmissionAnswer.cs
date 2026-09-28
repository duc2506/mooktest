using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MookTest.Model
{
    public class SubmissionAnswer
    {
        [Key]
        public int SubmissionAnswerId { get; set; }
        [Required]
        public int SubmissionId { get; set; }
        [ForeignKey(nameof(SubmissionId))]
        public QuizSubmission? QuizSubmission { get; set; }
        [Required]
        public int QuestionId { get; set; }
        [ForeignKey(nameof(QuestionId))]
        public Question? Question { get; set; }
        public int? AnswerId { get; set; }

        [ForeignKey(nameof(AnswerId))]

        public Answer? Answer { get; set; }
        public string? ResponseText { get; set; }

    }
}
