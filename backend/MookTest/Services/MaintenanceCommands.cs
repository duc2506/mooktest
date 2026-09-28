using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MookTest.Data;
using MookTest.DTO;
using MookTest.Model;

namespace MookTest.Services;

public static class MaintenanceCommands
{
    public static async Task<int> RunAsync(IServiceProvider services, IConfiguration configuration,
        bool migrate, bool createTrainer)
    {
        var stage = "validation";
        try
        {
            RegisterDto? dto = null;
            if (createTrainer)
            {
                dto = new RegisterDto {
                    Email = (configuration["Trainer:Email"] ?? "").Trim(),
                    DisplayName = (configuration["Trainer:DisplayName"] ?? "Trainer").Trim(),
                    Password = configuration["Trainer:Password"] ?? "" };
                // Validate before touching the database or applying migrations.
                var errors = new List<ValidationResult>();
                if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
                {
                    foreach (var error in errors) Console.Error.WriteLine($"ERROR [validation]: {error.ErrorMessage}");
                    return 2;
                }
            }

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            stage = "database configuration";
            if (string.IsNullOrWhiteSpace(db.Database.GetConnectionString()))
                throw new BusinessRuleException("Set ConnectionStrings:DefaultConnection in appsettings.json.");

            stage = "migrations";
            if (db.Database.HasPendingModelChanges())
                throw new BusinessRuleException("The EF model differs from the migration snapshot. Add or fix the migration before updating the database.");
            if (!migrate && !createTrainer)
            {
                Console.WriteLine("EF model matches the migration snapshot. No database connection was opened.");
                return 0;
            }
            if (migrate)
            {
                await db.Database.MigrateAsync();
                Console.WriteLine("Database migrations are up to date.");
            }
            else if ((await db.Database.GetPendingMigrationsAsync()).Any())
                throw new BusinessRuleException("Database migrations are pending. Run create-trainer.ps1 without -SkipMigrations, or run dotnet run --project backend/MookTest --launch-profile http -- --migrate first.");

            if (dto is not null)
            {
                stage = "create trainer";
                var normalizedEmail = dto.Email.ToUpperInvariant();
                if (await db.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail))
                    throw new BusinessRuleException("This email already exists. Sign in with the existing account or use another email. No account or password was changed.");
                await scope.ServiceProvider.GetRequiredService<AuthService>().RegisterAsync(dto, Roles.Trainer);
                Console.WriteLine("Trainer account created. You can now sign in with the email and password you entered.");
            }
            return 0;
        }
        catch (BusinessRuleException error)
        {
            Console.Error.WriteLine($"ERROR [{stage}]: {error.Message}");
            return 3;
        }
        catch (Exception error)
        {
            var cause = error.GetBaseException();
            if (cause is SqlException sql)
            {
                Console.Error.WriteLine($"ERROR [{stage}] SQL {sql.Number}: {sql.Message}");
                Console.Error.WriteLine("Check that SQL Server is running, the server/database name is correct, and your Windows account has database access.");
                if (sql.Number == 208)
                    Console.Error.WriteLine("A required table is missing. Check migration history and run --migrate before creating accounts.");
            }
            else Console.Error.WriteLine($"ERROR [{stage}] {cause.GetType().Name}: {cause.Message}");
            return 1;
        }
    }
}
