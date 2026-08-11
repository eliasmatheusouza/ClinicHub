using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;
using ClinicHub.Domain.Interfaces;
using MediatR;

namespace ClinicHub.Application.Patients.Queries.GetOwnPatientProfile;

public sealed class GetOwnPatientProfileQueryHandler(IPatientRepository patientRepository)
    : IRequestHandler<GetOwnPatientProfileQuery, ApplicationResult<PatientDto>>
{
    public async Task<ApplicationResult<PatientDto>> Handle(GetOwnPatientProfileQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return patient is null || !patient.IsActive
            ? ApplicationResult<PatientDto>.Failure(new("patient.profile.not_found", "Perfil de paciente não encontrado."))
            : ApplicationResult<PatientDto>.Success(PatientDto.FromDomain(patient));
    }
}
