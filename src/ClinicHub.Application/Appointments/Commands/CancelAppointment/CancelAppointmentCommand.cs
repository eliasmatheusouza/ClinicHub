using ClinicHub.Application.Appointments.Dtos;
using ClinicHub.Application.Common;

namespace ClinicHub.Application.Appointments.Commands.CancelAppointment;

public sealed record CancelAppointmentCommand(Guid AppointmentId, string Reason) : ICommand<ApplicationResult<AppointmentDto>>;
