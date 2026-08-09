namespace ClinicHub.API.Contracts.Appointments;

public sealed record ScheduleAppointmentRequest(Guid PatientId, Guid DoctorId, DateTime StartUtc, int DurationMinutes);
