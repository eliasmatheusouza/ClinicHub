using ClinicHub.Notifications.Worker.Messaging;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.AddHostedService<NotificationConsumerWorker>();
var host = builder.Build();
try
{
    await host.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
