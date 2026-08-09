using ClinicHub.Domain.Payments;

namespace ClinicHub.API.Contracts.Payments;

public sealed record RegisterPaymentRequest(Guid AppointmentId, decimal Amount, string Currency, PaymentMethod Method);
