namespace ClinicHub.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
