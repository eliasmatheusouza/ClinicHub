using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository(ClinicHubDbContext context) : IPaymentRepository
{
    public Task<bool> ExistsForAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default) =>
        context.Payments.AnyAsync(payment => payment.AppointmentId == appointmentId, cancellationToken);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
        context.Payments.AddAsync(payment, cancellationToken).AsTask();
}
