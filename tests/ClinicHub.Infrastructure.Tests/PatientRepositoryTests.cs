using ClinicHub.Domain.Patients;
using ClinicHub.Domain.ValueObjects;
using ClinicHub.Infrastructure.Persistence;
using ClinicHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Infrastructure.Tests;

public sealed class PatientRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsPersistedPatient()
    {
        var options = new DbContextOptionsBuilder<ClinicHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ClinicHubDbContext(options);
        var repository = new PatientRepository(context);
        var patient = Patient.Create(
            Guid.NewGuid(),
            PersonName.Create("Maria Silva").Value!,
            new DateOnly(1985, 5, 10),
            EmailAddress.Create("maria@clinichub.local").Value!,
            PhoneNumber.Create("11999999999").Value!,
            new DateOnly(2026, 8, 8)).Value!;

        await repository.AddAsync(patient);
        await context.SaveChangesAsync();
        var found = await repository.GetByIdAsync(patient.Id);

        Assert.NotNull(found);
        Assert.Equal(patient.Id, found.Id);
        Assert.Equal("maria@clinichub.local", found.Email.Value);
    }
}
