using System.Text;
using System.Text.Json;
using ClinicHub.Application.IntegrationEvents;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace ClinicHub.Infrastructure.Messaging;

internal sealed class RabbitMqIntegrationEventPublisher(IConfiguration configuration) : IIntegrationEventPublisher, IDisposable
{
    private const string ExchangeName = "clinichub.appointments";
    private readonly Lazy<IConnection> _connection = new(() =>
    {
        var connectionString = configuration["RabbitMq:ConnectionString"]
            ?? throw new InvalidOperationException("RabbitMq:ConnectionString deve ser configurada.");
        return new ConnectionFactory { Uri = new Uri(connectionString) }.CreateConnection("clinichub-api");
    });

    public Task PublishAsync(AppointmentConfirmedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var channel = _connection.Value.CreateModel();
        channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = integrationEvent.AppointmentId.ToString();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));
        channel.BasicPublish(ExchangeName, "appointment.confirmed", mandatory: false, basicProperties: properties, body: body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
