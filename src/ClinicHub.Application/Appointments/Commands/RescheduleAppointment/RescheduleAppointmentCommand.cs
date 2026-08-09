using ClinicHub.Application.Appointments.Dtos;
using ClinicHub.Application.Common;

namespace ClinicHub.Application.Appointments.Commands.RescheduleAppointment;

public sealed record RescheduleAppointmentCommand(Guid AppointmentId, DateTime StartUtc, int DurationMinutes)
    : ICommand<ApplicationResult<AppointmentDto>>;
