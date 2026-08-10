using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Authentication.Abstractions;
using ClinicHub.Application.Authentication.Commands.ConfirmEmail;
using ClinicHub.Application.Authentication.Commands.RegisterAccount;
using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;
using Moq;

namespace ClinicHub.Application.Tests;

public sealed class EmailRegistrationAndConfirmationHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RegisterAccount_WhenEmailIsAvailable_PersistsPendingPatientAndSendsConfirmation()
    {
        // Arrange
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        User? persistedUser = null;
        users.Setup(value => value.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => persistedUser = user)
            .Returns(Task.CompletedTask);
        var unitOfWork = UnitOfWork();
        var passwords = new Mock<IPasswordHashingService>();
        passwords.Setup(value => value.Hash("Password123!")).Returns("password-hash");
        var tokens = new Mock<IEmailConfirmationTokenService>();
        tokens.Setup(value => value.CreateToken()).Returns("confirmation-token");
        tokens.Setup(value => value.HashToken("confirmation-token")).Returns("confirmation-hash");
        var sender = new Mock<IEmailConfirmationSender>();
        sender.Setup(value => value.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new RegisterAccountCommandHandler(users.Object, unitOfWork.Object, passwords.Object, tokens.Object, sender.Object, new FixedClock());

        // Act
        var result = await handler.Handle(new("patient@clinichub.local", "Password123!"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(persistedUser);
        Assert.Equal(UserRole.Patient, persistedUser.Role);
        Assert.False(persistedUser.IsActive);
        Assert.Equal("confirmation-hash", persistedUser.EmailConfirmationTokenHash);
        Assert.Equal(Now.AddHours(24), persistedUser.EmailConfirmationExpiresAtUtc);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(value => value.SendAsync("patient@clinichub.local", "confirmation-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAccount_WhenEmailAlreadyExists_ReturnsErrorWithoutPersistingOrSendingEmail()
    {
        // Arrange
        var existingUser = ActiveUser();
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingUser);
        var passwords = new Mock<IPasswordHashingService>();
        var tokens = new Mock<IEmailConfirmationTokenService>();
        var sender = new Mock<IEmailConfirmationSender>();
        var unitOfWork = UnitOfWork();
        var handler = new RegisterAccountCommandHandler(users.Object, unitOfWork.Object, passwords.Object, tokens.Object, sender.Object, new FixedClock());

        // Act
        var result = await handler.Handle(new("patient@clinichub.local", "Password123!"), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "auth.email_already_registered");
        users.Verify(value => value.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        sender.Verify(value => value.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmEmail_WhenTokenMatchesPendingUser_ActivatesUserAndSavesChanges()
    {
        // Arrange
        var user = PendingUser();
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByEmailConfirmationTokenHashAsync("confirmation-hash", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tokens = new Mock<IEmailConfirmationTokenService>();
        tokens.Setup(value => value.HashToken("confirmation-token")).Returns("confirmation-hash");
        var unitOfWork = UnitOfWork();
        var handler = new ConfirmEmailCommandHandler(users.Object, unitOfWork.Object, tokens.Object, new FixedClock());

        // Act
        var result = await handler.Handle(new("confirmation-token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.Null(user.EmailConfirmationTokenHash);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_WhenTokenDoesNotIdentifyAUser_ReturnsErrorWithoutSavingChanges()
    {
        // Arrange
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByEmailConfirmationTokenHashAsync("unknown-hash", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var tokens = new Mock<IEmailConfirmationTokenService>();
        tokens.Setup(value => value.HashToken("unknown-token")).Returns("unknown-hash");
        var unitOfWork = UnitOfWork();
        var handler = new ConfirmEmailCommandHandler(users.Object, unitOfWork.Object, tokens.Object, new FixedClock());

        // Act
        var result = await handler.Handle(new("unknown-token"), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "auth.email_confirmation.invalid");
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static User ActiveUser() => User.Create(
        Guid.NewGuid(),
        EmailAddress.Create("patient@clinichub.local").Value!,
        "password-hash",
        UserRole.Patient).Value!;

    private static User PendingUser() => User.CreatePending(
        Guid.NewGuid(),
        EmailAddress.Create("patient@clinichub.local").Value!,
        "password-hash",
        UserRole.Patient,
        "confirmation-hash",
        Now.AddHours(24),
        Now).Value!;

    private static Mock<IUnitOfWork> UnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => Now;
    }
}
