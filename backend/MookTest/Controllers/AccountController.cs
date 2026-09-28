using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MookTest.Data;
using MookTest.Model;

namespace MookTest.Controllers;

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 10)] public string NewPassword { get; set; } = "";
}

[ApiController, Authorize, Route("api/auth")]
public class AccountController(AppDbContext db, IPasswordHasher<AppUser> hasher) : ControllerBase
{
    [HttpPost("change-password"), EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (!int.TryParse(User.FindFirstValue("sub"), out var userId)) return Unauthorized();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return Unauthorized();

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });
        if (request.CurrentPassword == request.NewPassword)
            return BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại." });

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
