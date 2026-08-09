using ClinicHub.Domain.Patients;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Tests;

public sealed class PatientTests
{
    [Fact]
    public void UpdateProfile_WhenPatientIsActive_UpdatesAllProperties()
    {
        var patient = CreatePatient();

        var result = patient.UpdateProfile(
            PersonName.Create("Ana Oliveira").Value!,
            new DateOnly(1991, 2, 3),
            EmailAddress.Create("ana.oliveira@clinichub.local").Value!,
            PhoneNumber.Create("21988887777").Value!,
            new DateOnly(2026, 8, 8));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana Oliveira", patient.Name.Value);
        Assert.Equal("ana.oliveira@clinichub.local", patient.Email.Value);
    }

    [Fact]
    public void Deactivate_Twice_ReturnsNotificationOnSecondAttempt()
    {
        var patient = CreatePatient();

        patient.Deactivate();
        var result = patient.Deactivate();

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Notifications, notification => notification.Code == "patient.inactive");
    }

    [Fact]
    public void Create_WhenBirthDateIsToday_ReturnsNotification()
    {
        var today = new DateOnly(2026, 8, 8);
        var result = Patient.Create(
            Guid.NewGuid(),
            PersonName.Create("Maria Silva").Value!,
            today,
            EmailAddress.Create("maria@clinichub.local").Value!,
            PhoneNumber.Create("11999999999").Value!,
            today);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Notifications, notification => notification.Code == "patient.birth_date.invalid");
    }

    private static Patient CreatePatient() => Patient.Create(
        Guid.NewGuid(),
        PersonName.Create("Maria Silva").Value!,
        new DateOnly(1985, 5, 10),
        EmailAddress.Create("maria@clinichub.local").Value!,
        PhoneNumber.Create("11999999999").Value!,
        new DateOnly(2026, 8, 8)).Value!;
}
