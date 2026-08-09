using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;
using ClinicHub.Domain.Interfaces;
using MediatR;

namespace ClinicHub.Application.Patients.Queries.GetPatientById;

public sealed class GetPatientByIdQueryHandler(IPatientRepository patientRepository)
    : IRequestHandler<GetPatientByIdQuery, ApplicationResult<PatientDto>>
{
    public async Task<ApplicationResult<PatientDto>> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken);

        return patient is null || !patient.IsActive
            ? ApplicationResult<PatientDto>.Failure(new("patient.not_found", "Paciente não encontrado."))
            : ApplicationResult<PatientDto>.Success(PatientDto.FromDomain(patient));
    }
}
