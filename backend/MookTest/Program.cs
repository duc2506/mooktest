using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MookTest.Data;
using MookTest.DTO;
using MookTest.Model;
using MookTest.Services;
using MookTest.Services.Interfaces;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args.Where(arg => arg is not ("--migrate" or "--create-trainer" or "--check-model")).ToArray());
var signingKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
        throw new InvalidOperationException("Configure Jwt:Key with at least 32 random bytes.");
    signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}
if (Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");
var jwt = new JwtSettings(builder.Configuration["Jwt:Issuer"] ?? "MookTest",
    builder.Configuration["Jwt:Audience"] ?? "MookTestFrontend",
    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)));
builder.Services.AddSingleton(jwt);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IQuizParticipationService, QuizParticipationService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = jwt.Key,
        ValidateLifetime = true, RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero, RoleClaimType = "role", NameClaimType = "name",
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
    };
});
builder.Services.AddAuthorization(options => options.FallbackPolicy =
    new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
// Maintenance commands only; normal startup never changes the database.
if (args.Contains("--migrate") || args.Contains("--create-trainer") || args.Contains("--check-model"))
{
    Environment.ExitCode = await MaintenanceCommands.RunAsync(app.Services, builder.Configuration,
        args.Contains("--migrate"), args.Contains("--create-trainer"));
    return;
}
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    context.Response.StatusCode = error switch {
        ArgumentException => 400, BusinessRuleException => 409, _ => 500 };
    await context.Response.WriteAsJsonAsync(new { message = context.Response.StatusCode == 500
        ? "Không thể xử lý yêu cầu. Vui lòng thử lại." : error!.Message });
}));
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}
else if (!app.Environment.IsEnvironment("Testing")) app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }


