namespace ClinicHub.API.Contracts.Authentication;

public sealed record RegisterAccountRequest(string Email, string Password, string ConfirmPassword);
