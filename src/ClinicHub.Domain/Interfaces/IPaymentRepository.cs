using ClinicHub.Domain.Payments;

namespace ClinicHub.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<bool> ExistsForAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
