namespace ClinicHub.Application.IntegrationEvents;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(AppointmentConfirmedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
