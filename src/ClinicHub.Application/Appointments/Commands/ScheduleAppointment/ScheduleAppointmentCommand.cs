using ClinicHub.Application.Appointments.Dtos;
using ClinicHub.Application.Common;

namespace ClinicHub.Application.Appointments.Commands.ScheduleAppointment;

public sealed record ScheduleAppointmentCommand(Guid PatientId, Guid DoctorId, DateTime StartUtc, int DurationMinutes)
    : ICommand<ApplicationResult<AppointmentDto>>;
