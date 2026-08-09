using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Authentication.Abstractions;
using ClinicHub.Application.Authentication.Dtos;
using ClinicHub.Application.Common;
using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;
using MediatR;

namespace ClinicHub.Application.Authentication.Commands.RegisterAccount;

public sealed class RegisterAccountCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPasswordHashingService passwordHashingService,
    IEmailConfirmationTokenService tokenService,
    IEmailConfirmationSender emailConfirmationSender,
    IClock clock) : IRequestHandler<RegisterAccountCommand, ApplicationResult<RegistrationResultDto>>
{
    public async Task<ApplicationResult<RegistrationResultDto>> Handle(RegisterAccountCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.Email);
        if (!emailResult.IsSuccess)
        {
            return ApplicationResult<RegistrationResultDto>.FailureFromDomain(emailResult.Notifications);
        }

        if (await userRepository.GetByEmailAsync(emailResult.Value!, cancellationToken) is not null)
        {
            return ApplicationResult<RegistrationResultDto>.Failure(new("auth.email_already_registered", "Este e-mail já possui uma conta."));
        }

        var now = clock.UtcNow;
        var confirmationToken = tokenService.CreateToken();
        var userResult = User.CreatePending(
            Guid.NewGuid(),
            emailResult.Value!,
            passwordHashingService.Hash(request.Password),
            UserRole.Patient,
            tokenService.HashToken(confirmationToken),
            now.AddHours(24),
            now);

        if (!userResult.IsSuccess)
        {
            return ApplicationResult<RegistrationResultDto>.FailureFromDomain(userResult.Notifications);
        }

        await userRepository.AddAsync(userResult.Value!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await emailConfirmationSender.SendAsync(emailResult.Value!.Value, confirmationToken, cancellationToken);

        return ApplicationResult<RegistrationResultDto>.Success(new("Conta criada. Confirme o link enviado para o seu e-mail em até 24 horas."));
    }
}
