using System.ComponentModel.DataAnnotations;

namespace MookTest.DTO;

public class LoginDto
{
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 10)] public string Password { get; set; } = "";
}

public class RegisterDto : LoginDto
{
    [Required, StringLength(100, MinimumLength = 2)] public string DisplayName { get; set; } = "";
}

public record UserDto(int Id, string Email, string DisplayName, string Role);
public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);
