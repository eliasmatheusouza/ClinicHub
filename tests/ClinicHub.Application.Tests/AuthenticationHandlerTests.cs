using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Authentication.Abstractions;
using ClinicHub.Application.Authentication.Commands.Login;
using ClinicHub.Application.Authentication.Commands.RefreshAccessToken;
using ClinicHub.Application.Authentication.Dtos;
using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;
using Moq;

namespace ClinicHub.Application.Tests;

public sealed class AuthenticationHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Login_WhenCredentialsAreValid_IssuesAndPersistsTokens()
    {
        var user = CreateUser();
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var refreshTokens = new Mock<IRefreshTokenRepository>();
        var unitOfWork = UnitOfWork();
        var passwordHasher = new Mock<IPasswordHashingService>();
        passwordHasher.Setup(value => value.Verify(user.PasswordHash, "Password123!")).Returns(true);
        var tokenIssuer = TokenIssuer();
        var handler = new LoginCommandHandler(users.Object, refreshTokens.Object, unitOfWork.Object, passwordHasher.Object, tokenIssuer.Object, new FixedClock());

        var result = await handler.Handle(new("doctor@clinichub.local", "Password123!"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value!.AccessToken);
        refreshTokens.Verify(value => value.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WhenPasswordIsInvalid_ReturnsGenericCredentialError()
    {
        var user = CreateUser();
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var passwordHasher = new Mock<IPasswordHashingService>();
        passwordHasher.Setup(value => value.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var handler = new LoginCommandHandler(users.Object, Mock.Of<IRefreshTokenRepository>(), Mock.Of<IUnitOfWork>(), passwordHasher.Object, TokenIssuer().Object, new FixedClock());

        var result = await handler.Handle(new("doctor@clinichub.local", "WrongPassword123!"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "auth.invalid_credentials");
    }

    [Fact]
    public async Task Refresh_WhenTokenIsActive_RotatesRefreshToken()
    {
        var user = CreateUser();
        var previousRefreshToken = RefreshToken.Create(Guid.NewGuid(), user.Id, "old-hash", Now.AddDays(1), Now).Value!;
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var refreshTokens = new Mock<IRefreshTokenRepository>();
        refreshTokens.Setup(value => value.GetByTokenHashAsync("old-hash", It.IsAny<CancellationToken>())).ReturnsAsync(previousRefreshToken);
        var tokenIssuer = TokenIssuer();
        tokenIssuer.Setup(value => value.HashRefreshToken("old-refresh-token")).Returns("old-hash");
        var handler = new RefreshAccessTokenCommandHandler(users.Object, refreshTokens.Object, UnitOfWork().Object, tokenIssuer.Object, new FixedClock());

        var result = await handler.Handle(new("old-refresh-token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(previousRefreshToken.RevokedAtUtc);
        refreshTokens.Verify(value => value.Update(previousRefreshToken), Times.Once);
        refreshTokens.Verify(value => value.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static User CreateUser() => User.Create(
        Guid.NewGuid(), EmailAddress.Create("doctor@clinichub.local").Value!, "password-hash", UserRole.Doctor).Value!;

    private static Mock<IUnitOfWork> UnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private static Mock<ITokenIssuer> TokenIssuer()
    {
        var tokenIssuer = new Mock<ITokenIssuer>();
        tokenIssuer.Setup(value => value.CreateRefreshToken()).Returns("new-refresh-token");
        tokenIssuer.Setup(value => value.HashRefreshToken("new-refresh-token")).Returns("new-hash");
        tokenIssuer.Setup(value => value.GetRefreshTokenExpiryUtc(Now)).Returns(Now.AddDays(7));
        tokenIssuer.Setup(value => value.CreateAccessToken(It.IsAny<User>())).Returns(new AccessTokenIssue("access-token", Now.AddMinutes(15)));
        return tokenIssuer;
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => Now;
    }
}
