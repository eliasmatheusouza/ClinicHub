using ClinicHub.Application.Common;
using ClinicHub.Application.Payments.Dtos;
using ClinicHub.Domain.Payments;

namespace ClinicHub.Application.Payments.Commands.RegisterPayment;

public sealed record RegisterPaymentCommand(Guid AppointmentId, decimal Amount, string Currency, PaymentMethod Method)
    : ICommand<ApplicationResult<PaymentDto>>;
