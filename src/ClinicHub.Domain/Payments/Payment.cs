using ClinicHub.Domain.Common;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Payments;

public sealed class Payment : AggregateRoot
{
    private Payment() : base()
    {
        Amount = null!;
    }

    private Payment(Guid id, Guid appointmentId, Money amount, PaymentMethod method, DateTime paidAtUtc) : base(id)
    {
        AppointmentId = appointmentId;
        Amount = amount;
        Method = method;
        PaidAtUtc = paidAtUtc;
    }

    public Guid AppointmentId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DateTime PaidAtUtc { get; private set; }

    public static DomainResult<Payment> Create(Guid id, Guid appointmentId, Money amount, PaymentMethod method, DateTime paidAtUtc, DateTime utcNow)
    {
        if (id == Guid.Empty || appointmentId == Guid.Empty)
        {
            return DomainResult<Payment>.Failure(new("payment.reference.required", "Pagamento e consulta devem ser identificados."));
        }

        if (paidAtUtc.Kind != DateTimeKind.Utc || paidAtUtc > utcNow)
        {
            return DomainResult<Payment>.Failure(new("payment.date.invalid", "A data de pagamento deve estar em UTC e não pode estar no futuro."));
        }

        return DomainResult<Payment>.Success(new Payment(id, appointmentId, amount, method, paidAtUtc));
    }
}
