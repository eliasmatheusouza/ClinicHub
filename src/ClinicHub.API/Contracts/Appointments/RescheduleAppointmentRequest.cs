namespace ClinicHub.API.Contracts.Appointments;

public sealed record RescheduleAppointmentRequest(DateTime StartUtc, int DurationMinutes);
