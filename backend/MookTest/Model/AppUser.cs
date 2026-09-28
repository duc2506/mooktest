using System.ComponentModel.DataAnnotations;

namespace MookTest.Model;

public static class Roles
{
    public const string Trainer = "Trainer";
    public const string Trainee = "Trainee";
}

public class AppUser
{
    public int Id { get; set; }
    [MaxLength(100)] public string DisplayName { get; set; } = "";
    [MaxLength(254)] public string Email { get; set; } = "";
    [MaxLength(254)] public string NormalizedEmail { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [MaxLength(20)] public string Role { get; set; } = Roles.Trainee;
}
