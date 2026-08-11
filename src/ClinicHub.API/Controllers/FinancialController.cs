using ClinicHub.Application.Financial.Queries.GetRevenueReport;
using ClinicHub.API.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicHub.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.FinancialRead)]
[Route("api/financial")]
public sealed class FinancialController(ISender sender) : ControllerBase
{
    [HttpGet("revenue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRevenue([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRevenueReportQuery(startDate, endDate), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }
}
