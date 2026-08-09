using FluentValidation;

namespace ClinicHub.Application.Authentication.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator() => RuleFor(command => command.Token).NotEmpty().MaximumLength(256);
}
