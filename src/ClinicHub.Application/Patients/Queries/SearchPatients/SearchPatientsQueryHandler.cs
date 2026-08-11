using ClinicHub.Application.Caching;
using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Patients;
using MediatR;

namespace ClinicHub.Application.Patients.Queries.SearchPatients;

public sealed class SearchPatientsQueryHandler(
    IPatientRepository patientRepository,
    IPatientListCache patientListCache) : IRequestHandler<SearchPatientsQuery, ApplicationResult<PagedResult<PatientListItemDto>>>
{
    public async Task<ApplicationResult<PagedResult<PatientListItemDto>>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken)
    {
        var normalizedTerm = request.Term?.Trim().ToLowerInvariant() ?? string.Empty;
        var cacheKey = $"patients:list:v{await patientListCache.GetVersionAsync(cancellationToken)}:term={normalizedTerm}:page={request.Page}:size={request.PageSize}";
        var cached = await patientListCache.GetAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return ApplicationResult<PagedResult<PatientListItemDto>>.Success(cached);
        }

        var searchResult = await patientRepository.SearchAsync(new PatientSearchFilter(request.Term, request.Page, request.PageSize), cancellationToken);
        var result = new PagedResult<PatientListItemDto>(
            searchResult.Items.Select(PatientListItemDto.FromDomain).ToArray(),
            request.Page,
            request.PageSize,
            searchResult.TotalCount);

        await patientListCache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return ApplicationResult<PagedResult<PatientListItemDto>>.Success(result);
    }
}
