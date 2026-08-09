using ClinicHub.Application.Authentication.Dtos;
using ClinicHub.Application.Common;

namespace ClinicHub.Application.Authentication.Commands.RegisterAccount;

public sealed record RegisterAccountCommand(string Email, string Password) : ICommand<ApplicationResult<RegistrationResultDto>>;
