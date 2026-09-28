using System.ComponentModel.DataAnnotations;

namespace MookTest.DTO;

public record ManagedUserDto(int Id, string Email, string DisplayName, string Role);

public class CreateManagedUserDto
{
    [Required, StringLength(100, MinimumLength = 2)] public string DisplayName { get; set; } = "";
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 10)] public string Password { get; set; } = "";
}

public class UpdateManagedUserDto
{
    [Required, StringLength(100, MinimumLength = 2)] public string DisplayName { get; set; } = "";
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
}
