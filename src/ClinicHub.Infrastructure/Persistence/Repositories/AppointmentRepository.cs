using ClinicHub.Domain.Appointments;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Infrastructure.Persistence.Repositories;

internal sealed class AppointmentRepository(ClinicHubDbContext context) : IAppointmentRepository
{
    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Appointments.SingleOrDefaultAsync(appointment => appointment.Id == id, cancellationToken);

    public Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default) =>
        context.Appointments.AddAsync(appointment, cancellationToken).AsTask();

    public void Update(Appointment appointment) => context.Appointments.Update(appointment);

    public Task<bool> HasConflictAsync(Guid doctorId, AppointmentSlot slot, Guid? ignoredAppointmentId = null, CancellationToken cancellationToken = default) =>
        context.Appointments.AnyAsync(
            appointment => appointment.DoctorId == doctorId
                           && appointment.Status != AppointmentStatus.Cancelled
                           && (!ignoredAppointmentId.HasValue || appointment.Id != ignoredAppointmentId.Value)
                           && appointment.Slot.StartUtc < slot.EndUtc
                           && appointment.Slot.EndUtc > slot.StartUtc,
            cancellationToken);
}
