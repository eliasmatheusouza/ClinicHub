using ClinicHub.Domain.Payments;

namespace ClinicHub.Application.Payments.Dtos;

public sealed record PaymentDto(
    Guid Id,
    Guid AppointmentId,
    decimal Amount,
    string Currency,
    string Method,
    DateTime PaidAtUtc)
{
    public static PaymentDto FromDomain(Payment payment) => new(
        payment.Id,
        payment.AppointmentId,
        payment.Amount.Amount,
        payment.Amount.Currency,
        payment.Method.ToString(),
        payment.PaidAtUtc);
}
