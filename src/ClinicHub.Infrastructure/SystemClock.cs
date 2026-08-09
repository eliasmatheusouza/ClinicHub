using ClinicHub.Application.Abstractions;

namespace ClinicHub.Infrastructure;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
