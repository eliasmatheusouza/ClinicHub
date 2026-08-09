using ClinicHub.Domain.Appointments;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid doctorId, AppointmentSlot slot, Guid? ignoredAppointmentId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    void Update(Appointment appointment);
}
