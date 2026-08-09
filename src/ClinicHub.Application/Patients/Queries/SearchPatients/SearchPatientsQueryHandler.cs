using ClinicHub.Application.Caching;
using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Patients;
using MediatR;

namespace ClinicHub.Application.Patients.Queries.SearchPatients;

public sealed class SearchPatientsQueryHandler(
    IPatientRepository patientRepository,
    IPatientListCache patientListCache) : IRequestHandler<SearchPatientsQuery, ApplicationResult<PagedResult<PatientDto>>>
{
    public async Task<ApplicationResult<PagedResult<PatientDto>>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken)
    {
        var normalizedTerm = request.Term?.Trim().ToLowerInvariant() ?? string.Empty;
        var cacheKey = $"patients:list:v{await patientListCache.GetVersionAsync(cancellationToken)}:term={normalizedTerm}:page={request.Page}:size={request.PageSize}";
        var cached = await patientListCache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return ApplicationResult<PagedResult<PatientDto>>.Success(cached);
        }

        var searchResult = await patientRepository.SearchAsync(new PatientSearchFilter(request.Term, request.Page, request.PageSize), cancellationToken);
        var result = new PagedResult<PatientDto>(
            searchResult.Items.Select(PatientDto.FromDomain).ToArray(),
            request.Page,
            request.PageSize,
            searchResult.TotalCount);

        await patientListCache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return ApplicationResult<PagedResult<PatientDto>>.Success(result);
    }
}
