using ClinicHub.Application.Authentication.Abstractions;
using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicHub.Infrastructure.Persistence;

public static class DevelopmentDatabaseInitializer
{
    public static async Task InitializeAsync(this IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled(configuration["Database:AutoMigrate"]))
        {
            return;
        }

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicHubDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        if (!IsEnabled(configuration["Seed:Enabled"]))
        {
            return;
        }

        var passwordHashingService = scope.ServiceProvider.GetRequiredService<IPasswordHashingService>();
        await EnsureUserAsync(
            context,
            passwordHashingService,
            configuration["Seed:AdminEmail"] ?? "admin@clinichub.local",
            configuration["Seed:AdminPassword"],
            UserRole.Admin,
            cancellationToken);
        await EnsureUserAsync(
            context,
            passwordHashingService,
            configuration["Seed:DoctorEmail"] ?? "doctor@clinichub.local",
            configuration["Seed:DoctorPassword"],
            UserRole.Doctor,
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureUserAsync(
        ClinicHubDbContext context,
        IPasswordHashingService passwordHashingService,
        string email,
        string? password,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(email);
        if (!emailResult.IsSuccess)
        {
            throw new InvalidOperationException($"O e-mail de seed para {role} é inválido.");
        }

        if (await context.Users.AnyAsync(user => user.Email.Value == emailResult.Value!.Value, cancellationToken))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException($"A senha de seed para {role} deve ser configurada.");
        }

        var userResult = User.Create(Guid.NewGuid(), emailResult.Value!, passwordHashingService.Hash(password), role);
        if (!userResult.IsSuccess)
        {
            throw new InvalidOperationException($"Não foi possível criar o usuário de seed para {role}.");
        }

        await context.Users.AddAsync(userResult.Value!, cancellationToken);
    }

    private static bool IsEnabled(string? value) => bool.TryParse(value, out var enabled) && enabled;
}
