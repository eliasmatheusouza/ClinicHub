using ClinicHub.Application.Appointments.Commands.ScheduleAppointment;

namespace ClinicHub.Application.Tests;

public sealed class AppointmentCommandValidatorTests
{
    [Fact]
    public void Validate_WhenDurationIsBelowMinimum_ReturnsError()
    {
        var validator = new ScheduleAppointmentCommandValidator();

        var command = new ScheduleAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(1), 10);
        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "DurationMinutes");
    }
}
