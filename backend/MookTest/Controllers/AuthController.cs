using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MookTest.Data;
using MookTest.DTO;
using MookTest.Services;

namespace MookTest.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(AuthService auth, AppDbContext db) : ControllerBase
{
    [AllowAnonymous, HttpPost("register"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterDto dto) => Ok(await auth.RegisterAsync(dto));

    [AllowAnonymous, HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await auth.LoginAsync(dto);
        return result is null ? Unauthorized(new { message = "Email hoặc mật khẩu không đúng." }) : Ok(result);
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.FindAsync(int.Parse(User.FindFirstValue("sub")!));
        return user is null ? Unauthorized() : Ok(new UserDto(user.Id, user.Email, user.DisplayName, user.Role));
    }
}
