using ClinicHub.Application.Common;

namespace ClinicHub.Application.Authentication.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token) : ICommand<ApplicationResult<bool>>;
