using FluentValidation;

namespace ClinicHub.Application.Appointments.Commands.ConfirmAppointment;

public sealed class ConfirmAppointmentCommandValidator : AbstractValidator<ConfirmAppointmentCommand>
{
    public ConfirmAppointmentCommandValidator()
    {
        RuleFor(command => command.AppointmentId).NotEmpty();
    }
}
