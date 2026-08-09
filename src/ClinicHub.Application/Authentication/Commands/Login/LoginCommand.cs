using ClinicHub.Application.Authentication.Dtos;
using ClinicHub.Application.Common;

namespace ClinicHub.Application.Authentication.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<ApplicationResult<AuthenticationTokensDto>>;
