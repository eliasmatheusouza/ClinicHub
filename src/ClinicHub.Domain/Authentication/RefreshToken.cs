using ClinicHub.Domain.Common;

namespace ClinicHub.Domain.Authentication;

public sealed class RefreshToken : AggregateRoot
{
    private RefreshToken() : base()
    {
        TokenHash = null!;
    }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime createdAtUtc) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public static DomainResult<RefreshToken> Create(Guid id, Guid userId, string? tokenHash, DateTime expiresAtUtc, DateTime createdAtUtc)
    {
        if (id == Guid.Empty || userId == Guid.Empty)
        {
            return DomainResult<RefreshToken>.Failure(new("refresh_token.reference.required", "Token e usuário devem ser identificados."));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return DomainResult<RefreshToken>.Failure(new("refresh_token.hash.required", "O hash do refresh token é obrigatório."));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc || expiresAtUtc.Kind != DateTimeKind.Utc || expiresAtUtc <= createdAtUtc)
        {
            return DomainResult<RefreshToken>.Failure(new("refresh_token.expiry.invalid", "A expiração do refresh token deve ser posterior à sua criação e estar em UTC."));
        }

        return DomainResult<RefreshToken>.Success(new RefreshToken(id, userId, tokenHash, expiresAtUtc, createdAtUtc));
    }

    public DomainResult Revoke(DateTime utcNow)
    {
        if (!IsActive(utcNow))
        {
            return DomainResult.Failure(new("refresh_token.inactive", "O refresh token não está ativo."));
        }

        RevokedAtUtc = utcNow;
        return DomainResult.Success();
    }
}
