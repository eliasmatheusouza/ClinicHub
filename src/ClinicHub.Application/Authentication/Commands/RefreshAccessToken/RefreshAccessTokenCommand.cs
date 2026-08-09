using ClinicHub.Application.Authentication.Dtos;
using ClinicHub.Application.Common;

namespace ClinicHub.Application.Authentication.Commands.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<ApplicationResult<AuthenticationTokensDto>>;
