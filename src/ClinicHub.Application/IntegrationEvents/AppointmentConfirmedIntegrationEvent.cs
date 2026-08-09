namespace ClinicHub.Application.IntegrationEvents;

public sealed record AppointmentConfirmedIntegrationEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime AppointmentStartUtc,
    DateTime OccurredOnUtc);
