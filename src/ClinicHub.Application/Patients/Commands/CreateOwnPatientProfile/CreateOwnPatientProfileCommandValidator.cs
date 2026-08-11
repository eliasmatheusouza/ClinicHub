using ClinicHub.Application.Abstractions;
using FluentValidation;

namespace ClinicHub.Application.Patients.Commands.CreateOwnPatientProfile;

public sealed class CreateOwnPatientProfileCommandValidator : AbstractValidator<CreateOwnPatientProfileCommand>
{
    public CreateOwnPatientProfileCommandValidator(IClock clock)
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.BirthDate).LessThan(DateOnly.FromDateTime(clock.UtcNow));
        RuleFor(command => command.Phone).NotEmpty().MaximumLength(25);
    }
}
