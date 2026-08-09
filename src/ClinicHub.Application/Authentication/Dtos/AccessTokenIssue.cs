namespace ClinicHub.Application.Authentication.Dtos;

public sealed record AccessTokenIssue(string Value, DateTime ExpiresAtUtc);
