using ClinicHub.API.Contracts.Payments;
using ClinicHub.Application.Payments.Commands.RegisterPayment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Receptionist")]
[Route("api/payments")]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterPaymentCommand(request.AppointmentId, request.Amount, request.Currency, request.Method), cancellationToken);
        if (result.IsSuccess)
        {
            return Created($"api/payments/{result.Value!.Id}", result.Value);
        }

        if (result.Errors.Any(error => error.Code == "appointment.not_found"))
        {
            return NotFound(new { errors = result.Errors });
        }

        return result.Errors.Any(error => error.Code is "payment.already_registered" or "payment.appointment_not_confirmed")
            ? Conflict(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }
}
