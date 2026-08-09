using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;

namespace ClinicHub.Application.Patients.Queries.SearchPatients;

public sealed record SearchPatientsQuery(string? Term, int Page = 1, int PageSize = 20) : IQuery<ApplicationResult<PagedResult<PatientDto>>>;
