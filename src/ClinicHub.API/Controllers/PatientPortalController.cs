using System.Security.Claims;
using ClinicHub.API.Authorization;
using ClinicHub.API.Contracts.Patients;
using ClinicHub.Application.Patients.Commands.CreateOwnPatientProfile;
using ClinicHub.Application.Patients.Commands.UpdateOwnPatientProfile;
using ClinicHub.Application.Patients.Queries.GetOwnPatientProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.PatientPortalAccess)]
[Route("api/patient-portal")]
public sealed class PatientPortalController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOwnPatientProfileQuery(UserId()), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    [HttpPost("me")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateMe(CreateOwnPatientProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateOwnPatientProfileCommand(UserId(), request.Name, request.BirthDate, request.Phone), cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetMe), result.Value);
        }

        return result.Errors.Any(error => error.Code is "patient.profile.already_exists" or "patient.profile.email_already_exists")
            ? Conflict(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe(UpdateOwnPatientProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateOwnPatientProfileCommand(UserId(), request.Name, request.BirthDate, request.Phone), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Errors.Any(error => error.Code == "patient.profile.not_found")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    private Guid UserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(rawUserId, out var userId)
            ? userId
            : throw new InvalidOperationException("O token autenticado não contém um identificador de usuário válido.");
    }
}
