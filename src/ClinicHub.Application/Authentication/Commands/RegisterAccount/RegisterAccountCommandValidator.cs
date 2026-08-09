using FluentValidation;

namespace ClinicHub.Application.Authentication.Commands.RegisterAccount;

public sealed class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Password)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("A senha deve conter uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve conter uma letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve conter um número.");
    }
}
