using ClinicHub.Domain.Appointments;
using ClinicHub.Domain.Events;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Tests;

public sealed class AppointmentTests
{
    private static readonly DateTime Now = new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Overlaps_WhenSlotsIntersect_ReturnsTrue()
    {
        var first = AppointmentSlot.Create(Now.AddHours(2), TimeSpan.FromMinutes(60)).Value!;
        var second = AppointmentSlot.Create(Now.AddHours(2).AddMinutes(30), TimeSpan.FromMinutes(30)).Value!;

        Assert.True(first.Overlaps(second));
    }

    [Fact]
    public void Confirm_WhenAppointmentIsScheduled_AddsDomainEvent()
    {
        var slot = AppointmentSlot.Create(Now.AddHours(2), TimeSpan.FromMinutes(30)).Value!;
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), slot, Now).Value!;

        var result = appointment.Confirm(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        var domainEvent = Assert.IsType<AppointmentConfirmedDomainEvent>(Assert.Single(appointment.DomainEvents));
        Assert.Equal(appointment.Id, domainEvent.AppointmentId);
    }

    [Fact]
    public void Cancel_WhenReasonIsMissing_ReturnsNotification()
    {
        var slot = AppointmentSlot.Create(Now.AddHours(2), TimeSpan.FromMinutes(30)).Value!;
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), slot, Now).Value!;

        var result = appointment.Cancel(" ");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Notifications, notification => notification.Code == "appointment.cancellation_reason.invalid");
    }

    [Fact]
    public void Reschedule_WhenConfirmed_ReturnsAppointmentToScheduled()
    {
        var appointment = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AppointmentSlot.Create(Now.AddHours(2), TimeSpan.FromMinutes(30)).Value!, Now).Value!;
        appointment.Confirm(Now);

        var result = appointment.Reschedule(AppointmentSlot.Create(Now.AddHours(4), TimeSpan.FromMinutes(45)).Value!, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.Equal(45, appointment.Slot.Duration.TotalMinutes);
    }
}
