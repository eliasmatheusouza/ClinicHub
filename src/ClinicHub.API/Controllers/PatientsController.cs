using ClinicHub.API.Contracts.Patients;
using ClinicHub.Application.Patients.Commands.CreatePatient;
using ClinicHub.Application.Patients.Commands.DeactivatePatient;
using ClinicHub.Application.Patients.Commands.UpdatePatient;
using ClinicHub.Application.Patients.Queries.GetPatientById;
using ClinicHub.Application.Patients.Queries.SearchPatients;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Doctor,Receptionist")]
[Route("api/patients")]
public sealed class PatientsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePatientCommand(request.Name, request.BirthDate, request.Email, request.Phone), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string? term, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchPatientsQuery(term, page, pageSize), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPatientByIdQuery(id), cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Errors.Any(error => error.Code == "patient.not_found")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Receptionist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdatePatientCommand(id, request.Name, request.BirthDate, request.Email, request.Phone), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Errors.Any(error => error.Code == "patient.not_found")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivatePatientCommand(id), cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.Errors.Any(error => error.Code == "patient.not_found")
            ? NotFound(new { errors = result.Errors })
            : BadRequest(new { errors = result.Errors });
    }
}
