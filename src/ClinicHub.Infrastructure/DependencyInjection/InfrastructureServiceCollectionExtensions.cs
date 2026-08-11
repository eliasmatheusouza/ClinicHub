using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Auditing;
using ClinicHub.Application.Authentication.Abstractions;
using ClinicHub.Application.Caching;
using ClinicHub.Application.Events;
using ClinicHub.Application.Financial.Abstractions;
using ClinicHub.Application.IntegrationEvents;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Infrastructure.Authentication;
using ClinicHub.Infrastructure.Auditing;
using ClinicHub.Infrastructure.Caching;
using ClinicHub.Infrastructure.Messaging;
using ClinicHub.Infrastructure.Financial;
using ClinicHub.Infrastructure.Persistence;
using ClinicHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ClinicHub.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("A connection string 'SqlServer' deve ser configurada.");

        services.AddDbContext<ClinicHubDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditTrailWriter, EfAuditTrailWriter>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IEmailConfirmationTokenService, EmailConfirmationTokenService>();
        services.AddScoped<IEmailConfirmationSender, EmailConfirmationSender>();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConnectionString = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("A connection string 'Redis' deve ser configurada.");
            var options = ConfigurationOptions.Parse(redisConnectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<IPatientListCache, RedisPatientListCache>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
        services.AddScoped<IRevenueReportReader, DapperRevenueReportReader>();

        return services;
    }
}
