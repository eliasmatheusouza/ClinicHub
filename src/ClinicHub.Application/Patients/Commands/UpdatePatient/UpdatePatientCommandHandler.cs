using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Caching;
using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;
using ClinicHub.Domain.Common;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.ValueObjects;
using MediatR;

namespace ClinicHub.Application.Patients.Commands.UpdatePatient;

public sealed class UpdatePatientCommandHandler(
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IPatientListCache patientListCache,
    IClock clock) : IRequestHandler<UpdatePatientCommand, ApplicationResult<PatientDto>>
{
    public async Task<ApplicationResult<PatientDto>> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return ApplicationResult<PatientDto>.Failure(new("patient.not_found", "Paciente não encontrado."));
        }

        var nameResult = PersonName.Create(request.Name);
        var emailResult = EmailAddress.Create(request.Email);
        var phoneResult = PhoneNumber.Create(request.Phone);
        var validationErrors = new DomainResult[] { nameResult, emailResult, phoneResult }
            .Where(result => !result.IsSuccess)
            .SelectMany(result => result.Notifications)
            .ToArray();
        if (validationErrors.Length > 0)
        {
            return ApplicationResult<PatientDto>.FailureFromDomain(validationErrors);
        }

        if (await patientRepository.ExistsByEmailAsync(emailResult.Value!, request.PatientId, cancellationToken))
        {
            return ApplicationResult<PatientDto>.Failure(new("patient.email.already_exists", "Já existe um paciente cadastrado com este e-mail."));
        }

        var updateResult = patient.UpdateProfile(nameResult.Value!, request.BirthDate, emailResult.Value!, phoneResult.Value!, DateOnly.FromDateTime(clock.UtcNow));
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
