using ClinicHub.Application.Appointments.Dtos;
using ClinicHub.Application.Common;

namespace ClinicHub.Application.Appointments.Commands.ConfirmAppointment;

public sealed record ConfirmAppointmentCommand(Guid AppointmentId) : ICommand<ApplicationResult<AppointmentDto>>;
