using System.Text.Json;
using System.Threading.RateLimiting;
using ClinicHub.API.ExceptionHandling;
using ClinicHub.API.HealthChecks;
using ClinicHub.API.Middleware;
using ClinicHub.API.Swagger;
using ClinicHub.Application.DependencyInjection;
using ClinicHub.Infrastructure.DependencyInjection;
using ClinicHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ClinicHub.API")
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
        .AllowAnyHeader()
        .AllowAnyMethod()));
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth-login", context => CreateAuthRateLimitPolicy(context, builder.Configuration, "Login", 5, 60));
        options.AddPolicy("auth-register", context => CreateAuthRateLimitPolicy(context, builder.Configuration, "Register", 3, 600));
        options.AddPolicy("auth-confirm-email", context => CreateAuthRateLimitPolicy(context, builder.Configuration, "ConfirmEmail", 10, 60));
        options.AddPolicy("auth-refresh", context => CreateAuthRateLimitPolicy(context, builder.Configuration, "Refresh", 10, 60));
    });
    builder.Services.AddControllers();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "ClinicHub API", Version = "v1" });
        options.OperationFilter<RequestExampleOperationFilter>();
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
        });
    });

    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key deve ser configurado.");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer deve ser configurado.");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience deve ser configurado.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        });

    var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("A connection string 'Redis' deve ser configurada.");
    var rabbitMqConnectionString = builder.Configuration["RabbitMq:ConnectionString"]
        ?? throw new InvalidOperationException("A connection string do RabbitMQ deve ser configurada.");

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ClinicHubDbContext>("sqlserver", tags: ["ready"])
        .AddRedis(redisConnectionString, name: "redis", tags: ["ready"])
        .AddRabbitMQ(rabbitMqConnectionString, name: "rabbitmq", tags: ["ready"]);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        await app.Services.InitializeAsync(app.Configuration);
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseCors("frontend");
    app.UseRateLimiter();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    });

    app.Run();
}
catch (Exception exception) when (exception.GetType().Name == "HostAbortedException")
{
    // O EF Core Tools encerra o host depois de localizar o DbContext.
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static RateLimitPartition<string> CreateAuthRateLimitPolicy(
    HttpContext context,
    IConfiguration configuration,
    string policyName,
    int defaultPermitLimit,
    int defaultWindowSeconds)
{
    var section = configuration.GetSection($"RateLimiting:{policyName}");
    var permitLimit = section.GetValue<int?>("PermitLimit") ?? defaultPermitLimit;
    var windowSeconds = section.GetValue<int?>("WindowSeconds") ?? defaultWindowSeconds;
    var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        });
}

public partial class Program;
