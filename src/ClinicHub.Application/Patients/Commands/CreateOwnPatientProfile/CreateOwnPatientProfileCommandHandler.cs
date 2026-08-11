using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Caching;
using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;
using ClinicHub.Domain.Common;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Patients;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;
using MediatR;

namespace ClinicHub.Application.Patients.Commands.CreateOwnPatientProfile;

public sealed class CreateOwnPatientProfileCommandHandler(
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IPatientListCache patientListCache,
    IClock clock) : IRequestHandler<CreateOwnPatientProfileCommand, ApplicationResult<PatientDto>>
{
    public async Task<ApplicationResult<PatientDto>> Handle(CreateOwnPatientProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.Role != UserRole.Patient)
        {
            return ApplicationResult<PatientDto>.Failure(new("patient.profile.account_invalid", "A conta do paciente não está disponível."));
        }

        if (await patientRepository.GetByUserIdAsync(request.UserId, cancellationToken) is not null)
        {
            return ApplicationResult<PatientDto>.Failure(new("patient.profile.already_exists", "Esta conta já possui um perfil de paciente."));
        }

        var nameResult = PersonName.Create(request.Name);
        var phoneResult = PhoneNumber.Create(request.Phone);
        var validationErrors = new DomainResult[] { nameResult, phoneResult }
            .Where(result => !result.IsSuccess)
            .SelectMany(result => result.Notifications)
            .ToArray();
        if (validationErrors.Length > 0)
        {
            return ApplicationResult<PatientDto>.FailureFromDomain(validationErrors);
        }

        if (await patientRepository.ExistsByEmailAsync(user.Email, cancellationToken: cancellationToken))
        {
            return ApplicationResult<PatientDto>.Failure(new("patient.profile.email_already_exists", "Já existe um prontuário com o e-mail desta conta. Solicite a vinculação à clínica."));
        }

        var patientResult = Patient.Create(
            Guid.NewGuid(),
            nameResult.Value!,
            request.BirthDate,
            user.Email,
            phoneResult.Value!,
            DateOnly.FromDateTime(clock.UtcNow),
            request.UserId);
        if (!patientResult.IsSuccess)
        {
            return ApplicationResult<PatientDto>.FailureFromDomain(patientResult.Notifications);
        }

        await patientRepository.AddAsync(patientResult.Value!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await patientListCache.InvalidateAsync(cancellationToken);
        return ApplicationResult<PatientDto>.Success(PatientDto.FromDomain(patientResult.Value!));
    }
}
