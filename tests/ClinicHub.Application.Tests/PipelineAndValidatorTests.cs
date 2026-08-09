using ClinicHub.Application.Appointments.Commands.CancelAppointment;
using ClinicHub.Application.Appointments.Commands.ConfirmAppointment;
using ClinicHub.Application.Appointments.Commands.RescheduleAppointment;
using ClinicHub.Application.Authentication.Commands.Login;
using ClinicHub.Application.Authentication.Commands.RefreshAccessToken;
using ClinicHub.Application.Common;
using ClinicHub.Application.Common.Behaviors;
using ClinicHub.Application.Events;
using ClinicHub.Application.IntegrationEvents;
using ClinicHub.Application.Patients.Queries.GetPatientById;
using ClinicHub.Application.Patients.Queries.SearchPatients;
using ClinicHub.Application.Payments.Commands.RegisterPayment;
using ClinicHub.Domain.Events;
using ClinicHub.Domain.Payments;
using FluentValidation;
using MediatR;
using Moq;

namespace ClinicHub.Application.Tests;

public sealed class PipelineAndValidatorTests
{
    [Fact]
    public async Task ValidationBehavior_WhenInvalid_ReturnsErrorsWithoutInvokingNext()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(value => value.Name).NotEmpty();
        var behavior = new ValidationBehavior<TestRequest, ApplicationResult<string>>([validator]);
        var nextWasCalled = false;

        var result = await behavior.Handle(new(""), () =>
        {
            nextWasCalled = true;
            return Task.FromResult(ApplicationResult<string>.Success("ok"));
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(nextWasCalled);
        Assert.Contains(result.Errors, error => error.Code == "validation.Name");
    }

    [Fact]
    public async Task AppointmentConfirmedEventHandler_PublishesIntegrationEvent()
    {
        var publisher = new Mock<IIntegrationEventPublisher>();
        publisher.Setup(value => value.PublishAsync(It.IsAny<AppointmentConfirmedIntegrationEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new AppointmentConfirmedDomainEventHandler(publisher.Object);
        var domainEvent = new AppointmentConfirmedDomainEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow);

        await handler.Handle(new DomainEventNotification(domainEvent), CancellationToken.None);

        publisher.Verify(value => value.PublishAsync(It.Is<AppointmentConfirmedIntegrationEvent>(integrationEvent => integrationEvent.AppointmentId == domainEvent.AppointmentId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Validators_RejectInvalidRequests()
    {
        Assert.False(new LoginCommandValidator().Validate(new LoginCommand("invalid", "short")).IsValid);
        Assert.False(new RefreshAccessTokenCommandValidator().Validate(new RefreshAccessTokenCommand("")).IsValid);
        Assert.False(new ConfirmAppointmentCommandValidator().Validate(new ConfirmAppointmentCommand(Guid.Empty)).IsValid);
        Assert.False(new RescheduleAppointmentCommandValidator().Validate(new RescheduleAppointmentCommand(Guid.Empty, default, 1)).IsValid);
        Assert.False(new CancelAppointmentCommandValidator().Validate(new CancelAppointmentCommand(Guid.Empty, "")).IsValid);
        Assert.False(new GetPatientByIdQueryValidator().Validate(new GetPatientByIdQuery(Guid.Empty)).IsValid);
        Assert.False(new SearchPatientsQueryValidator().Validate(new SearchPatientsQuery(null, 0, 101)).IsValid);
        Assert.False(new RegisterPaymentCommandValidator().Validate(new RegisterPaymentCommand(Guid.Empty, 0, "", (PaymentMethod)999)).IsValid);
    }

    private sealed record TestRequest(string Name) : IRequest<ApplicationResult<string>>;
}
