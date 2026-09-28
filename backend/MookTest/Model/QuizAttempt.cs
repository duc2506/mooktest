namespace MookTest.Model;

public class QuizAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
