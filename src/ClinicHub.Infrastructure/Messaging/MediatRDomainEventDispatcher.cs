using ClinicHub.Application.Events;
using ClinicHub.Domain.Common;
using MediatR;

namespace ClinicHub.Infrastructure.Messaging;

internal sealed class MediatRDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) =>
        Task.WhenAll(domainEvents.Select(domainEvent => publisher.Publish(new DomainEventNotification(domainEvent), cancellationToken)));
}
