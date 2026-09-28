using System.ComponentModel.DataAnnotations;
namespace MookTest.Model
{
    public class Quiz
    {
        [Key]
        public int QuizId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        [Required]
        public int Duration { get; set; }
        public bool ShowAnswersAfterSubmit { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<QuizSubmission> QuizSubmissions { get; set; } = new List<QuizSubmission>();
    }
}
