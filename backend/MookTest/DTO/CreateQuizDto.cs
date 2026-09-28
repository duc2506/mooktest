using System.ComponentModel.DataAnnotations;
namespace MookTest.DTO
{
    public class CreateQuizDto
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Range(1, 10080)]
        public int Duration { get; set; }
    }
}

