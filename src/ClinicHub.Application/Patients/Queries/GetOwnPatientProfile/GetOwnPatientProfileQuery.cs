using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;

namespace ClinicHub.Application.Patients.Queries.GetOwnPatientProfile;

public sealed record GetOwnPatientProfileQuery(Guid UserId) : IQuery<ApplicationResult<PatientDto>>;
