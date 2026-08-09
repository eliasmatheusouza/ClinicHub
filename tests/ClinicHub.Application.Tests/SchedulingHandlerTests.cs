using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Appointments.Commands.CancelAppointment;
using ClinicHub.Application.Appointments.Commands.RescheduleAppointment;
using ClinicHub.Application.Appointments.Commands.ScheduleAppointment;
using ClinicHub.Domain.Appointments;
using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Patients;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;
using Moq;

namespace ClinicHub.Application.Tests;

public sealed class SchedulingHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Schedule_WhenSlotIsAvailable_CreatesAppointment()
    {
        var patient = Patient.Create(Guid.NewGuid(), PersonName.Create("Maria Silva").Value!, new DateOnly(1985, 1, 1), EmailAddress.Create("maria@clinichub.local").Value!, PhoneNumber.Create("11999999999").Value!, DateOnly.FromDateTime(Now)).Value!;
        var doctor = User.Create(Guid.NewGuid(), EmailAddress.Create("doctor@clinichub.local").Value!, "hash", UserRole.Doctor).Value!;
        var patients = new Mock<IPatientRepository>();
        patients.Setup(value => value.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        var users = new Mock<IUserRepository>();
        users.Setup(value => value.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        var appointments = new Mock<IAppointmentRepository>();
        appointments.Setup(value => value.HasConflictAsync(doctor.Id, It.IsAny<AppointmentSlot>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new ScheduleAppointmentCommandHandler(patients.Object, users.Object, appointments.Object, unitOfWork.Object, new FixedClock());

        var result = await handler.Handle(new(patient.Id, doctor.Id, Now.AddHours(2), 30), CancellationToken.None);

        Assert.True(result.IsSuccess);
        appointments.Verify(value => value.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reschedule_WhenSlotConflicts_ReturnsConflictError()
    {
        var appointment = CreateAppointment();
        var repository = new Mock<IAppointmentRepository>();
        repository.Setup(value => value.GetByIdAsync(appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        repository.Setup(value => value.HasConflictAsync(appointment.DoctorId, It.IsAny<AppointmentSlot>(), appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new RescheduleAppointmentCommandHandler(repository.Object, Mock.Of<IUnitOfWork>(), new FixedClock());

        var result = await handler.Handle(new(appointment.Id, Now.AddHours(3), 30), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "appointment.conflict");
    }

    [Fact]
    public async Task Cancel_WhenReasonIsValid_CancelsAppointment()
    {
        var appointment = CreateAppointment();
        var repository = new Mock<IAppointmentRepository>();
        repository.Setup(value => value.GetByIdAsync(appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new CancelAppointmentCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(new(appointment.Id, "Paciente solicitou cancelamento"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Cancelled.ToString(), result.Value!.Status);
    }

    private static Appointment CreateAppointment() => Appointment.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AppointmentSlot.Create(Now.AddHours(2), TimeSpan.FromMinutes(30)).Value!, Now).Value!;

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => Now;
    }
}
