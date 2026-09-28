using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MookTest.Data;
using MookTest.DTO;
using MookTest.Model;

namespace MookTest.Services;

public record JwtSettings(string Issuer, string Audience, SymmetricSecurityKey Key);

public class AuthService(AppDbContext db, IPasswordHasher<AppUser> hasher, JwtSettings jwt)
{
    public async Task<AuthResponse> RegisterAsync(RegisterDto dto, string role = Roles.Trainee)
    {
        var email = dto.Email.Trim();
        var normalized = email.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized))
            throw new BusinessRuleException("Email nÃ y Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng.");
        var user = new AppUser { Email = email, NormalizedEmail = normalized,
            DisplayName = dto.DisplayName.Trim(), Role = role };
        user.PasswordHash = hasher.HashPassword(user, dto.Password);
        db.Users.Add(user);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            if (await db.Users.AsNoTracking().AnyAsync(u => u.NormalizedEmail == normalized))
                throw new BusinessRuleException("Email nÃ y Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng.");
            throw;
        }
        return IssueToken(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginDto dto)
    {
        var normalized = dto.Email.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalized);
        if (user is null) return null;
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, dto.Password);
            await db.SaveChangesAsync();
        }
        return IssueToken(user);
    }

    private AuthResponse IssueToken(AppUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(60);
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
             new Claim("role", user.Role), new Claim("name", user.DisplayName)],
            notBefore: DateTime.UtcNow, expires: expires,
            signingCredentials: new SigningCredentials(jwt.Key, SecurityAlgorithms.HmacSha256));
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires,
            new UserDto(user.Id, user.Email, user.DisplayName, user.Role));
    }
}

