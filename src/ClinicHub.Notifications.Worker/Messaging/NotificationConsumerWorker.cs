using System.Text;
using System.Text.Json;
using ClinicHub.Application.IntegrationEvents;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ClinicHub.Notifications.Worker.Messaging;

public sealed class NotificationConsumerWorker(IConfiguration configuration, ILogger<NotificationConsumerWorker> logger) : BackgroundService
{
    private const string ExchangeName = "clinichub.appointments";
    private const string QueueName = "clinichub.notifications.appointment-confirmed";
    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration["RabbitMq:ConnectionString"]
            ?? throw new InvalidOperationException("RabbitMq:ConnectionString deve ser configurada.");
        var factory = new ConnectionFactory { Uri = new Uri(connectionString), DispatchConsumersAsync = true };
        _connection = factory.CreateConnection("clinichub-notifications-worker");
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(QueueName, ExchangeName, "appointment.confirmed");
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += ProcessMessageAsync;
        _channel.BasicConsume(QueueName, autoAck: false, consumer);

        logger.LogInformation("Notification worker is consuming queue {QueueName}", QueueName);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private Task ProcessMessageAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            var message = JsonSerializer.Deserialize<AppointmentConfirmedIntegrationEvent>(Encoding.UTF8.GetString(eventArgs.Body.ToArray()));
            if (message is null)
            {
                throw new InvalidOperationException("Mensagem de consulta confirmada inválida.");
            }

            logger.LogInformation(
                "Simulating appointment confirmation notification. AppointmentId: {AppointmentId}, PatientId: {PatientId}, DoctorId: {DoctorId}, StartUtc: {StartUtc}",
                message.AppointmentId,
                message.PatientId,
                message.DoctorId,
                message.AppointmentStartUtc);
            _channel!.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process notification message {DeliveryTag}", eventArgs.DeliveryTag);
            _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
