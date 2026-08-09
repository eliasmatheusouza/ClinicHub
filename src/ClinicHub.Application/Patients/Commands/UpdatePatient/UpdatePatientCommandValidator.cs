using ClinicHub.Application.Abstractions;
using FluentValidation;

namespace ClinicHub.Application.Patients.Commands.UpdatePatient;

public sealed class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator(IClock clock)
    {
        RuleFor(command => command.PatientId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.BirthDate).LessThan(DateOnly.FromDateTime(clock.UtcNow));
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Phone).NotEmpty().MaximumLength(25);
    }
}
