using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Caching;
using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;
using ClinicHub.Domain.Common;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Patients;
using ClinicHub.Domain.ValueObjects;
using MediatR;

namespace ClinicHub.Application.Patients.Commands.CreatePatient;

public sealed class CreatePatientCommandHandler(
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IPatientListCache patientListCache,
    IClock clock) : IRequestHandler<CreatePatientCommand, ApplicationResult<PatientDto>>
{
    public async Task<ApplicationResult<PatientDto>> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
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

        if (await patientRepository.ExistsByEmailAsync(emailResult.Value!, cancellationToken: cancellationToken))
        {
            return ApplicationResult<PatientDto>.Failure(new("patient.email.already_exists", "Já existe um paciente cadastrado com este e-mail."));
        }

        var patientResult = Patient.Create(
            Guid.NewGuid(),
            nameResult.Value!,
            request.BirthDate,
            emailResult.Value!,
            phoneResult.Value!,
            DateOnly.FromDateTime(clock.UtcNow));

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
