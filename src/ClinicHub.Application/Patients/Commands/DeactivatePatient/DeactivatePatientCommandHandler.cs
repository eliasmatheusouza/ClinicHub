using ClinicHub.Application.Caching;
using ClinicHub.Application.Common;
using ClinicHub.Domain.Interfaces;
using MediatR;

namespace ClinicHub.Application.Patients.Commands.DeactivatePatient;

public sealed class DeactivatePatientCommandHandler(
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IPatientListCache patientListCache) : IRequestHandler<DeactivatePatientCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> Handle(DeactivatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return ApplicationResult<Guid>.Failure(new("patient.not_found", "Paciente não encontrado."));
        }

        var deactivateResult = patient.Deactivate();
        if (!deactivateResult.IsSuccess)
        {
            return ApplicationResult<Guid>.FailureFromDomain(deactivateResult.Notifications);
        }

        patientRepository.Update(patient);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await patientListCache.InvalidateAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(patient.Id);
    }
}
