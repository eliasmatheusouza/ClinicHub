using ClinicHub.API.Contracts.Appointments;
using ClinicHub.API.Authorization;
using ClinicHub.Application.Appointments.Commands.CancelAppointment;
using ClinicHub.Application.Appointments.Commands.ConfirmAppointment;
using ClinicHub.Application.Appointments.Commands.RescheduleAppointment;
using ClinicHub.Application.Appointments.Commands.ScheduleAppointment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AppointmentsManage)]
[Route("api/appointments")]
public sealed class AppointmentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Schedule(ScheduleAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ScheduleAppointmentCommand(request.PatientId, request.DoctorId, request.StartUtc, request.DurationMinutes), cancellationToken);
        return result.IsSuccess
            ? Created($"api/appointments/{result.Value!.Id}", result.Value)
            : ToErrorResult(result.Errors);
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmAppointmentCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result.Errors);
    }

    [HttpPut("{id:guid}/schedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reschedule(Guid id, RescheduleAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RescheduleAppointmentCommand(id, request.StartUtc, request.DurationMinutes), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result.Errors);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelAppointmentCommand(id, request.Reason), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result.Errors);
    }

    private IActionResult ToErrorResult(IReadOnlyCollection<ClinicHub.Application.Common.ApplicationError> errors)
    {
        if (errors.Any(error => error.Code == "appointment.not_found" || error.Code == "patient.not_found" || error.Code == "doctor.not_found"))
        {
            return NotFound(new { errors });
        }

        return errors.Any(error => error.Code == "appointment.conflict")
            ? Conflict(new { errors })
            : BadRequest(new { errors });
    }
}
