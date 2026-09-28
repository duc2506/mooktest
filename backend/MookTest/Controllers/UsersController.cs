using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MookTest.Data;
using MookTest.DTO;
using MookTest.Model;

namespace MookTest.Controllers;

[ApiController, Route("api/users"), Authorize(Roles = Roles.Trainer)]
public class UsersController(AppDbContext db, IPasswordHasher<AppUser> hasher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await db.Users.AsNoTracking()
        .Where(u => u.Role == Roles.Trainee).OrderBy(u => u.DisplayName)
        .Select(u => new ManagedUserDto(u.Id, u.Email, u.DisplayName, u.Role)).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CreateManagedUserDto dto)
    {
        var email = dto.Email.Trim();
        var normalized = email.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized))
            return Conflict(new { message = "Email này đã được sử dụng." });
        var user = new AppUser { Email = email, NormalizedEmail = normalized,
            DisplayName = dto.DisplayName.Trim(), Role = Roles.Trainee };
        user.PasswordHash = hasher.HashPassword(user, dto.Password);
        db.Users.Add(user); await db.SaveChangesAsync();
        return Ok(new ManagedUserDto(user.Id, user.Email, user.DisplayName, user.Role));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateManagedUserDto dto)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id && u.Role == Roles.Trainee);
        if (user is null) return NotFound(new { message = "Không tìm thấy người dùng." });
        var normalized = dto.Email.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.Id != id && u.NormalizedEmail == normalized))
            return Conflict(new { message = "Email này đã được sử dụng." });
        user.DisplayName = dto.DisplayName.Trim(); user.Email = dto.Email.Trim(); user.NormalizedEmail = normalized;
        await db.SaveChangesAsync();
        return Ok(new ManagedUserDto(user.Id, user.Email, user.DisplayName, user.Role));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id && u.Role == Roles.Trainee);
        if (user is null) return NotFound(new { message = "Không tìm thấy người dùng." });
        if (await db.QuizSubmissions.AnyAsync(s => s.UserId == id) || await db.QuizAttempts.AnyAsync(a => a.UserId == id))
            return Conflict(new { message = "Không thể xóa học viên đã có lịch sử làm bài." });
        db.Users.Remove(user); await db.SaveChangesAsync(); return NoContent();
    }
}
