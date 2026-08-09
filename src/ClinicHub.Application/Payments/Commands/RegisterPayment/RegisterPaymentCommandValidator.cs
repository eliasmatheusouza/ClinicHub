using ClinicHub.Domain.Payments;
using FluentValidation;

namespace ClinicHub.Application.Payments.Commands.RegisterPayment;

public sealed class RegisterPaymentCommandValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentCommandValidator()
    {
        RuleFor(command => command.AppointmentId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: false);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.Method).IsInEnum();
    }
}
