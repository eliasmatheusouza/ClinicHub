using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Caching;
using ClinicHub.Application.Patients.Commands.CreatePatient;
using ClinicHub.Domain.Interfaces;
using Moq;

namespace ClinicHub.Application.Tests;

public sealed class CreatePatientCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenInputIsValid_PersistsPatientAndInvalidatesCache()
    {
        var patientRepository = new Mock<IPatientRepository>();
        patientRepository.Setup(repository => repository.ExistsByEmailAsync(It.IsAny<ClinicHub.Domain.ValueObjects.EmailAddress>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var cache = new Mock<IPatientListCache>();
        cache.Setup(value => value.InvalidateAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new CreatePatientCommandHandler(patientRepository.Object, unitOfWork.Object, cache.Object, new FixedClock());

        var result = await handler.Handle(new("Ana Souza", new DateOnly(1990, 1, 1), "ana@clinichub.local", "11999999999"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ana@clinichub.local", result.Value!.Email);
        patientRepository.Verify(repository => repository.AddAsync(It.IsAny<ClinicHub.Domain.Patients.Patient>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(value => value.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);
    }
}
