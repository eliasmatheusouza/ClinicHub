using ClinicHub.Application.IntegrationEvents;
using ClinicHub.Domain.Events;
using MediatR;

namespace ClinicHub.Application.Events;

public sealed class AppointmentConfirmedDomainEventHandler(IIntegrationEventPublisher integrationEventPublisher)
    : INotificationHandler<DomainEventNotification>
{
    public Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        if (notification.DomainEvent is not AppointmentConfirmedDomainEvent domainEvent)
        {
            return Task.CompletedTask;
        }

        return integrationEventPublisher.PublishAsync(
            new(domainEvent.AppointmentId, domainEvent.PatientId, domainEvent.DoctorId, domainEvent.AppointmentStartUtc, domainEvent.OccurredOnUtc),
            cancellationToken);
    }
}
