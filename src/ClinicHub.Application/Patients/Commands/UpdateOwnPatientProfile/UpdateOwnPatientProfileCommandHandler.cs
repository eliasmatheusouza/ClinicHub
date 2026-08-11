using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Caching;
using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;
using ClinicHub.Domain.Common;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.ValueObjects;
using MediatR;

namespace ClinicHub.Application.Patients.Commands.UpdateOwnPatientProfile;

public sealed class UpdateOwnPatientProfileCommandHandler(
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IPatientListCache patientListCache,
    IClock clock) : IRequestHandler<UpdateOwnPatientProfileCommand, ApplicationResult<PatientDto>>
{
    public async Task<ApplicationResult<PatientDto>> Handle(UpdateOwnPatientProfileCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (patient is null || !patient.IsActive)
        {
            return ApplicationResult<PatientDto>.Failure(new("patient.profile.not_found", "Perfil de paciente não encontrado."));
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

        var updateResult = patient.UpdateProfile(
            nameResult.Value!,
            request.BirthDate,
            patient.Email,
            phoneResult.Value!,
            DateOnly.FromDateTime(clock.UtcNow));
        if (!updateResult.IsSuccess)
        {
            return ApplicationResult<PatientDto>.FailureFromDomain(updateResult.Notifications);
        }

        patientRepository.Update(patient);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await patientListCache.InvalidateAsync(cancellationToken);
        return ApplicationResult<PatientDto>.Success(PatientDto.FromDomain(patient));
    }
}
