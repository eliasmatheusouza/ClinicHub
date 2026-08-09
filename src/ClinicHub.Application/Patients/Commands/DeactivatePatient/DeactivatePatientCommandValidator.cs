using FluentValidation;

namespace ClinicHub.Application.Patients.Commands.DeactivatePatient;

public sealed class DeactivatePatientCommandValidator : AbstractValidator<DeactivatePatientCommand>
{
    public DeactivatePatientCommandValidator()
    {
        RuleFor(command => command.PatientId).NotEmpty();
    }
}
