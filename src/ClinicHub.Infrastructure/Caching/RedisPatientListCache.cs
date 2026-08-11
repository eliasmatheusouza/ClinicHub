using System.Text.Json;
using ClinicHub.Application.Caching;
using ClinicHub.Application.Patients.Dtos;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ClinicHub.Infrastructure.Caching;

internal sealed class RedisPatientListCache(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisPatientListCache> logger) : IPatientListCache
{
    private const string VersionKey = "patients:list:version";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<PagedResult<PatientListItemDto>?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var value = await _database.StringGetAsync(key);
            return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<PagedResult<PatientListItemDto>>(value!, SerializerOptions);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis unavailable while reading patient list cache. Key: {CacheKey}", key);
            return null;
        }
    }

    public async Task SetAsync(string key, PagedResult<PatientListItemDto> value, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _database.StringSetAsync(key, JsonSerializer.Serialize(value, SerializerOptions), timeToLive);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis unavailable while writing patient list cache. Key: {CacheKey}", key);
        }
    }

    public async Task<long> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var value = await _database.StringGetAsync(VersionKey);
            if (value.IsNullOrEmpty)
            {
                await _database.StringSetAsync(VersionKey, 0, when: When.NotExists);
                value = await _database.StringGetAsync(VersionKey);
            }

            return (long)value!;
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis unavailable while retrieving patient list cache version.");
            return 0;
        }
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _database.StringIncrementAsync(VersionKey);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis unavailable while invalidating patient list cache.");
        }
    }
}
