using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Authentication.Abstractions;
using ClinicHub.Application.Common;
using ClinicHub.Domain.Interfaces;
using MediatR;

namespace ClinicHub.Application.Authentication.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IEmailConfirmationTokenService tokenService,
    IClock clock) : IRequestHandler<ConfirmEmailCommand, ApplicationResult<bool>>
{
    public async Task<ApplicationResult<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashToken(request.Token);
        var user = await userRepository.GetByEmailConfirmationTokenHashAsync(tokenHash, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<bool>.Failure(new("auth.email_confirmation.invalid", "O link de confirmação é inválido ou já foi utilizado."));
        }

        var confirmationResult = user.ConfirmEmail(tokenHash, clock.UtcNow);
        if (!confirmationResult.IsSuccess)
        {
            return ApplicationResult<bool>.FailureFromDomain(confirmationResult.Notifications);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<bool>.Success(true);
    }
}
