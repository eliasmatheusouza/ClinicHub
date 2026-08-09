using FluentValidation;

namespace ClinicHub.Application.Appointments.Commands.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(command => command.AppointmentId).NotEmpty();
        RuleFor(command => command.StartUtc).NotEqual(default(DateTime));
        RuleFor(command => command.DurationMinutes).InclusiveBetween(15, 480);
    }
}
