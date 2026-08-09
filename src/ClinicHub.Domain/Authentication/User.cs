using ClinicHub.Domain.Common;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Authentication;

public sealed class User : AggregateRoot
{
    private User() : base()
    {
        Email = null!;
        PasswordHash = null!;
    }

    private User(Guid id, EmailAddress email, string passwordHash, UserRole role, bool isActive) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = isActive;
    }

    public EmailAddress Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public string? EmailConfirmationTokenHash { get; private set; }
    public DateTime? EmailConfirmationExpiresAtUtc { get; private set; }
    public DateTime? EmailConfirmedAtUtc { get; private set; }

    public static DomainResult<User> Create(Guid id, EmailAddress email, string? passwordHash, UserRole role)
    {
        if (id == Guid.Empty)
        {
            return DomainResult<User>.Failure(new("user.id.required", "O identificador do usuário é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return DomainResult<User>.Failure(new("user.password_hash.required", "A credencial do usuário é obrigatória."));
        }

        if (!Enum.IsDefined(role))
        {
            return DomainResult<User>.Failure(new("user.role.invalid", "O perfil do usuário é inválido."));
        }

        return DomainResult<User>.Success(new User(id, email, passwordHash, role, true));
    }

    public static DomainResult<User> CreatePending(
        Guid id,
        EmailAddress email,
        string? passwordHash,
        UserRole role,
        string? emailConfirmationTokenHash,
        DateTime emailConfirmationExpiresAtUtc,
        DateTime utcNow)
    {
        var userResult = Create(id, email, passwordHash, role);
        if (!userResult.IsSuccess)
        {
            return userResult;
        }

        if (string.IsNullOrWhiteSpace(emailConfirmationTokenHash))
        {
            return DomainResult<User>.Failure(new("user.email_confirmation_token.required", "O token de confirmação é obrigatório."));
        }

        if (utcNow.Kind != DateTimeKind.Utc || emailConfirmationExpiresAtUtc.Kind != DateTimeKind.Utc || emailConfirmationExpiresAtUtc <= utcNow)
        {
            return DomainResult<User>.Failure(new("user.email_confirmation_expiry.invalid", "A expiração da confirmação de e-mail deve ser futura e estar em UTC."));
        }

        var user = userResult.Value!;
        user.IsActive = false;
        user.EmailConfirmationTokenHash = emailConfirmationTokenHash;
        user.EmailConfirmationExpiresAtUtc = emailConfirmationExpiresAtUtc;
        return DomainResult<User>.Success(user);
    }

    public DomainResult ConfirmEmail(string tokenHash, DateTime utcNow)
    {
        if (IsActive || EmailConfirmedAtUtc is not null || string.IsNullOrWhiteSpace(EmailConfirmationTokenHash) || EmailConfirmationExpiresAtUtc is null)
        {
            return DomainResult.Failure(new("user.email_confirmation.invalid", "O link de confirmação é inválido ou já foi utilizado."));
        }

        if (utcNow.Kind != DateTimeKind.Utc || EmailConfirmationExpiresAtUtc <= utcNow)
        {
            return DomainResult.Failure(new("user.email_confirmation.expired", "O link de confirmação expirou."));
        }

        if (!string.Equals(EmailConfirmationTokenHash, tokenHash, StringComparison.Ordinal))
        {
            return DomainResult.Failure(new("user.email_confirmation.invalid", "O link de confirmação é inválido ou já foi utilizado."));
        }

        IsActive = true;
        EmailConfirmedAtUtc = utcNow;
        EmailConfirmationTokenHash = null;
        EmailConfirmationExpiresAtUtc = null;
        return DomainResult.Success();
    }
}
