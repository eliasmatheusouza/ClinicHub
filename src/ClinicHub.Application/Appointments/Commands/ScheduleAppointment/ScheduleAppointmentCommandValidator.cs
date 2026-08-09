using FluentValidation;

namespace ClinicHub.Application.Appointments.Commands.ScheduleAppointment;

public sealed class ScheduleAppointmentCommandValidator : AbstractValidator<ScheduleAppointmentCommand>
{
    public ScheduleAppointmentCommandValidator()
    {
        RuleFor(command => command.PatientId).NotEmpty();
        RuleFor(command => command.DoctorId).NotEmpty();
        RuleFor(command => command.StartUtc).NotEqual(default(DateTime));
        RuleFor(command => command.DurationMinutes).InclusiveBetween(15, 480);
    }
}
