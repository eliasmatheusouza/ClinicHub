using ClinicHub.Domain.Common;

namespace ClinicHub.Domain.ValueObjects;

public sealed record AppointmentSlot
{
    private AppointmentSlot()
    {
    }

    private AppointmentSlot(DateTime startUtc, TimeSpan duration)
    {
        StartUtc = startUtc;
        Duration = duration;
        EndUtc = startUtc.Add(duration);
    }

    public DateTime StartUtc { get; private set; }
    public TimeSpan Duration { get; private set; }
    public DateTime EndUtc { get; private set; }

    public bool Overlaps(AppointmentSlot other) => StartUtc < other.EndUtc && EndUtc > other.StartUtc;

    public static DomainResult<AppointmentSlot> Create(DateTime startUtc, TimeSpan duration)
    {
        if (startUtc.Kind != DateTimeKind.Utc)
        {
            return DomainResult<AppointmentSlot>.Failure(new("appointment_slot.timezone", "O horário da consulta deve estar em UTC."));
        }

        if (duration < TimeSpan.FromMinutes(15) || duration > TimeSpan.FromHours(8))
        {
            return DomainResult<AppointmentSlot>.Failure(new("appointment_slot.duration", "A duração deve estar entre 15 minutos e 8 horas."));
        }

        return DomainResult<AppointmentSlot>.Success(new AppointmentSlot(startUtc, duration));
    }
}
