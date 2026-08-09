using ClinicHub.Application.Patients.Dtos;

namespace ClinicHub.Application.Caching;

public interface IPatientListCache
{
    Task<PagedResult<PatientDto>?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, PagedResult<PatientDto> value, TimeSpan timeToLive, CancellationToken cancellationToken = default);
    Task<long> GetVersionAsync(CancellationToken cancellationToken = default);
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
