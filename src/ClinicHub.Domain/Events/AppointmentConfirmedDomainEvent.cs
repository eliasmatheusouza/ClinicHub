using ClinicHub.Domain.Common;

namespace ClinicHub.Domain.Events;

public sealed record AppointmentConfirmedDomainEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime AppointmentStartUtc,
    DateTime OccurredOnUtc) : IDomainEvent;
