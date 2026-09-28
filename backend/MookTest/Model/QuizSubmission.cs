using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MookTest.Model
{
    public class QuizSubmission
    {
        [Key]
        public int QuizSubmissionId { get; set; }
        [Required]
        public int QuizId { get; set; }
        public int? UserId { get; set; }
        public AppUser? User { get; set; }
        public Guid? AttemptId { get; set; }
        public QuizAttempt? Attempt { get; set; }

        [ForeignKey(nameof(QuizId))]
        public Quiz? Quiz { get; set; }
        [Required]
        public DateTime SubmittedAt { get; set; }
        public ICollection<SubmissionAnswer> SubmissionAnswers { get; set; } = new List<SubmissionAnswer>();

    }
}
