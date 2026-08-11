using ClinicHub.API.Contracts.Authentication;
using ClinicHub.Application.Authentication.Commands.ConfirmEmail;
using ClinicHub.Application.Authentication.Commands.Login;
using ClinicHub.Application.Authentication.Commands.RegisterAccount;
using ClinicHub.Application.Authentication.Commands.RefreshAccessToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicHub.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return ToActionResult(result.IsSuccess, result.Value, result.Errors);
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterAccountRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest(new { errors = new[] { new { code = "auth.password_confirmation_mismatch", message = "A confirmação de senha não corresponde." } } });
        }

        var result = await sender.Send(new RegisterAccountCommand(request.Email, request.Password), cancellationToken);
        return result.IsSuccess
            ? Accepted(value: result.Value)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("confirm-email")]
    [EnableRateLimiting("auth-confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmEmailCommand(request.Token), cancellationToken);
        return result.IsSuccess
            ? Ok(new { message = "E-mail confirmado. Você já pode entrar." })
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshAccessTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshAccessTokenCommand(request.RefreshToken), cancellationToken);
        return ToActionResult(result.IsSuccess, result.Value, result.Errors);
    }

    private IActionResult ToActionResult<T>(bool isSuccess, T? value, IReadOnlyCollection<ClinicHub.Application.Common.ApplicationError> errors)
    {
        if (isSuccess)
        {
            return Ok(value);
        }

        return errors.Any(error => error.Code.StartsWith("validation.", StringComparison.Ordinal))
            ? BadRequest(new { errors })
            : Unauthorized(new { errors });
    }
}
