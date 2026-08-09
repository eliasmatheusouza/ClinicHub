using ClinicHub.Domain.Appointments;

namespace ClinicHub.Application.Appointments.Dtos;

public sealed record AppointmentDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    DateTime StartUtc,
    DateTime EndUtc,
    int DurationMinutes,
    string Status,
    string? CancellationReason)
{
    public static AppointmentDto FromDomain(Appointment appointment) => new(
        appointment.Id,
        appointment.PatientId,
        appointment.DoctorId,
        appointment.Slot.StartUtc,
        appointment.Slot.EndUtc,
        (int)appointment.Slot.Duration.TotalMinutes,
        appointment.Status.ToString(),
        appointment.CancellationReason);
}
