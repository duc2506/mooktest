using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using MookTest.Model;
namespace MookTest.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<QuizSubmission> QuizSubmissions { get; set; }
        public DbSet<SubmissionAnswer> SubmissionAnswers { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AppUser>().HasIndex(u => u.NormalizedEmail).IsUnique();
            modelBuilder.Entity<QuizAttempt>().HasOne(a => a.Quiz).WithMany()
                .HasForeignKey(a => a.QuizId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<QuizAttempt>().HasOne(a => a.User).WithMany()
                .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<QuizSubmission>().HasOne(s => s.User).WithMany()
                .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<QuizSubmission>().HasIndex(s => new { s.UserId, s.QuizId });
            modelBuilder.Entity<QuizSubmission>().HasOne(s => s.Attempt).WithOne()
                .HasForeignKey<QuizSubmission>(s => s.AttemptId).OnDelete(DeleteBehavior.Restrict);

            // quiz 1 - n question
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Question>()
                .HasIndex(q => new { q.QuizId, q.BankQuestionId })
                .IsUnique()
                .HasFilter("[QuizId] IS NOT NULL AND [BankQuestionId] IS NOT NULL");

            // Quiz 1 - N QuizSubmission
            modelBuilder.Entity<QuizSubmission>()
                .HasOne(s => s.Quiz)
                .WithMany(q => q.QuizSubmissions)
                .HasForeignKey(s => s.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuizSubmission 1 - N SubmissionAnswer
            modelBuilder.Entity<SubmissionAnswer>()
                .HasOne(s => s.QuizSubmission)
                .WithMany(s => s.SubmissionAnswers)
                .HasForeignKey(s => s.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question 1 - N SubmissionAnswer
            modelBuilder.Entity<SubmissionAnswer>()
                .HasOne(s => s.Question)
                .WithMany(q => q.SubmissionsAnswers)
                .HasForeignKey(s => s.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Answer 1 - N SubmissionAnswer
            modelBuilder.Entity<SubmissionAnswer>()
                .HasOne(s => s.Answer)
                .WithMany(a => a.SubmissionAnswers)
                .HasForeignKey(s => s.AnswerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
