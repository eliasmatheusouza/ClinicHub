using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Caching;
using ClinicHub.Application.Patients.Commands.CreateOwnPatientProfile;
using ClinicHub.Application.Patients.Queries.GetOwnPatientProfile;
using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Patients;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;
using Moq;

namespace ClinicHub.Application.Tests;

public sealed class PatientPortalHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateOwnProfile_WhenPatientAccountIsValid_CreatesProfileLinkedToAuthenticatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(PatientUser(userId));
        var patients = new Mock<IPatientRepository>();
        patients.Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);
        patients.Setup(repository => repository.ExistsByEmailAsync(It.IsAny<EmailAddress>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Patient? persistedPatient = null;
        patients.Setup(repository => repository.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()))
            .Callback<Patient, CancellationToken>((patient, _) => persistedPatient = patient)
            .Returns(Task.CompletedTask);
        var unitOfWork = UnitOfWork();
        var cache = Cache();
        var handler = new CreateOwnPatientProfileCommandHandler(patients.Object, users.Object, unitOfWork.Object, cache.Object, new FixedClock());

        // Act
        var result = await handler.Handle(new(userId, "Ana Souza", new DateOnly(1990, 1, 1), "11999999999"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(persistedPatient);
        Assert.Equal(userId, persistedPatient.UserId);
        Assert.Equal("patient@clinichub.local", persistedPatient.Email.Value);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(value => value.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOwnProfile_WhenProfileAlreadyBelongsToUser_RejectsSecondProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(PatientUser(userId));
        var patients = new Mock<IPatientRepository>();
        patients.Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(PatientProfile(userId));
        var unitOfWork = UnitOfWork();
        var handler = new CreateOwnPatientProfileCommandHandler(patients.Object, users.Object, unitOfWork.Object, Cache().Object, new FixedClock());

        // Act
        var result = await handler.Handle(new(userId, "Ana Souza", new DateOnly(1990, 1, 1), "11999999999"), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "patient.profile.already_exists");
        patients.Verify(repository => repository.AddAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOwnProfile_WhenAnotherUserIdIsRequested_DoesNotReturnOtherPatientsProfile()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var patients = new Mock<IPatientRepository>();
        patients.Setup(repository => repository.GetByUserIdAsync(ownerUserId, It.IsAny<CancellationToken>())).ReturnsAsync(PatientProfile(ownerUserId));
        patients.Setup(repository => repository.GetByUserIdAsync(anotherUserId, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);
        var handler = new GetOwnPatientProfileQueryHandler(patients.Object);

        // Act
        var result = await handler.Handle(new(anotherUserId), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "patient.profile.not_found");
        patients.Verify(repository => repository.GetByUserIdAsync(anotherUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static User PatientUser(Guid userId) => User.Create(
        userId,
        EmailAddress.Create("patient@clinichub.local").Value!,
        "password-hash",
        UserRole.Patient).Value!;

    private static Patient PatientProfile(Guid userId) => Patient.Create(
        Guid.NewGuid(),
        PersonName.Create("Ana Souza").Value!,
        new DateOnly(1990, 1, 1),
        EmailAddress.Create("patient@clinichub.local").Value!,
        PhoneNumber.Create("11999999999").Value!,
        DateOnly.FromDateTime(Now),
        userId).Value!;

    private static Mock<IUnitOfWork> UnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private static Mock<IPatientListCache> Cache()
    {
        var cache = new Mock<IPatientListCache>();
        cache.Setup(value => value.InvalidateAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return cache;
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => Now;
    }
}
