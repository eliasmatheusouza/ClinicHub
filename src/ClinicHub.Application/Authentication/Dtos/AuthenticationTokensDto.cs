namespace ClinicHub.Application.Authentication.Dtos;

public sealed record AuthenticationTokensDto(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken);
