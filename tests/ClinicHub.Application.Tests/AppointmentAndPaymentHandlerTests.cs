using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Appointments.Commands.ConfirmAppointment;
using ClinicHub.Application.Events;
using ClinicHub.Application.Payments.Commands.RegisterPayment;
using ClinicHub.Domain.Appointments;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Payments;
using ClinicHub.Domain.ValueObjects;
using Moq;

namespace ClinicHub.Application.Tests;

public sealed class AppointmentAndPaymentHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ConfirmAppointment_WhenScheduled_PersistsAndDispatchesDomainEvent()
    {
        var appointment = CreateAppointment();
        var appointmentRepository = new Mock<IAppointmentRepository>();
        appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        dispatcher.Setup(value => value.DispatchAsync(It.IsAny<IEnumerable<ClinicHub.Domain.Common.IDomainEvent>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new ConfirmAppointmentCommandHandler(appointmentRepository.Object, unitOfWork.Object, dispatcher.Object, new FixedClock());

        var result = await handler.Handle(new(appointment.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Confirmed.ToString(), result.Value!.Status);
        appointmentRepository.Verify(repository => repository.Update(appointment), Times.Once);
        dispatcher.Verify(value => value.DispatchAsync(It.IsAny<IEnumerable<ClinicHub.Domain.Common.IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterPayment_WhenAppointmentIsConfirmed_PersistsPayment()
    {
        var appointment = CreateAppointment();
        appointment.Confirm(Now);
        var appointmentRepository = new Mock<IAppointmentRepository>();
        appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var paymentRepository = new Mock<IPaymentRepository>();
        paymentRepository.Setup(repository => repository.ExistsForAppointmentAsync(appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new RegisterPaymentCommandHandler(appointmentRepository.Object, paymentRepository.Object, unitOfWork.Object, new FixedClock());

        var result = await handler.Handle(new(appointment.Id, 150m, "BRL", PaymentMethod.Pix), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(150m, result.Value!.Amount);
        paymentRepository.Verify(repository => repository.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterPayment_WhenAppointmentIsNotConfirmed_ReturnsBusinessError()
    {
        var appointment = CreateAppointment();
        var appointmentRepository = new Mock<IAppointmentRepository>();
        appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var handler = new RegisterPaymentCommandHandler(appointmentRepository.Object, Mock.Of<IPaymentRepository>(), Mock.Of<IUnitOfWork>(), new FixedClock());

        var result = await handler.Handle(new(appointment.Id, 150m, "BRL", PaymentMethod.Pix), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "payment.appointment_not_confirmed");
    }

    private static Appointment CreateAppointment()
    {
        var slot = AppointmentSlot.Create(Now.AddHours(2), TimeSpan.FromMinutes(30)).Value!;
        return Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), slot, Now).Value!;
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => Now;
    }
}
