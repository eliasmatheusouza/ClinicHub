using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;

namespace ClinicHub.Application.Patients.Queries.GetPatientById;

public sealed record GetPatientByIdQuery(Guid PatientId) : IQuery<ApplicationResult<PatientDto>>;
