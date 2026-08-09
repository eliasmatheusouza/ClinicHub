using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Caching;
using ClinicHub.Application.Patients.Commands.DeactivatePatient;
using ClinicHub.Application.Patients.Commands.UpdatePatient;
using ClinicHub.Application.Patients.Dtos;
using ClinicHub.Application.Patients.Queries.SearchPatients;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Patients;
using ClinicHub.Domain.ValueObjects;
using Moq;

namespace ClinicHub.Application.Tests;

public sealed class PatientHandlerTests
{
    [Fact]
    public async Task UpdatePatient_WhenValid_UpdatesAndInvalidatesLists()
    {
        var patient = CreatePatient();
        var repository = new Mock<IPatientRepository>();
        repository.Setup(value => value.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        repository.Setup(value => value.ExistsByEmailAsync(It.IsAny<EmailAddress>(), patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var unitOfWork = UnitOfWork();
        var cache = Cache();
        var handler = new UpdatePatientCommandHandler(repository.Object, unitOfWork.Object, cache.Object, new FixedClock());

        var result = await handler.Handle(new(patient.Id, "Ana Souza", new DateOnly(1992, 2, 2), "ana@clinichub.local", "21999999999"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Souza", result.Value!.Name);
        repository.Verify(value => value.Update(patient), Times.Once);
        cache.Verify(value => value.InvalidateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivatePatient_WhenFound_MarksPatientInactive()
    {
        var patient = CreatePatient();
        var repository = new Mock<IPatientRepository>();
        repository.Setup(value => value.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        var handler = new DeactivatePatientCommandHandler(repository.Object, UnitOfWork().Object, Cache().Object);

        var result = await handler.Handle(new(patient.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(patient.IsActive);
    }

    [Fact]
    public async Task SearchPatients_WhenCacheMiss_QueriesRepositoryAndStoresPage()
    {
        var patient = CreatePatient();
        var repository = new Mock<IPatientRepository>();
        repository.Setup(value => value.SearchAsync(It.IsAny<PatientSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientSearchResult([patient], 1));
        var cache = Cache();
        cache.Setup(value => value.GetVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        cache.Setup(value => value.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((PagedResult<PatientDto>?)null);
        cache.Setup(value => value.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<PatientDto>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new SearchPatientsQueryHandler(repository.Object, cache.Object);

        var result = await handler.Handle(new("maria", 1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        cache.Verify(value => value.SetAsync(It.Is<string>(key => key.Contains("v2")), It.IsAny<PagedResult<PatientDto>>(), TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Patient CreatePatient() => Patient.Create(
        Guid.NewGuid(),
        PersonName.Create("Maria Silva").Value!,
        new DateOnly(1985, 5, 10),
        EmailAddress.Create("maria@clinichub.local").Value!,
        PhoneNumber.Create("11999999999").Value!,
        new DateOnly(2026, 8, 8)).Value!;

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
        public DateTime UtcNow => new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);
    }
}
