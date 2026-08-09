using ClinicHub.Application.Users.Queries.GetDoctors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Receptionist")]
[Route("api/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("doctors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDoctorsQuery(), cancellationToken);
        return Ok(result.Value);
    }
}
