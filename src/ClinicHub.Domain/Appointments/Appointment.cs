using ClinicHub.Domain.Common;
using ClinicHub.Domain.Events;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Appointments;

public sealed class Appointment : AggregateRoot
{
    private Appointment() : base()
    {
        Slot = null!;
    }

    private Appointment(Guid id, Guid patientId, Guid doctorId, AppointmentSlot slot) : base(id)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        Slot = slot;
        Status = AppointmentStatus.Scheduled;
    }

    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public AppointmentSlot Slot { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }

    public static DomainResult<Appointment> Create(Guid id, Guid patientId, Guid doctorId, AppointmentSlot slot, DateTime utcNow)
    {
        if (id == Guid.Empty || patientId == Guid.Empty || doctorId == Guid.Empty)
        {
            return DomainResult<Appointment>.Failure(new("appointment.reference.required", "Consulta, paciente e médico devem ser identificados."));
        }

        if (slot.StartUtc <= utcNow)
        {
            return DomainResult<Appointment>.Failure(new("appointment.slot.past", "Não é permitido agendar consultas no passado."));
        }

        return DomainResult<Appointment>.Success(new Appointment(id, patientId, doctorId, slot));
    }

    public DomainResult Confirm(DateTime utcNow)
    {
        if (Status == AppointmentStatus.Cancelled)
        {
            return DomainResult.Failure(new("appointment.cancelled", "Uma consulta cancelada não pode ser confirmada."));
        }

        if (Status == AppointmentStatus.Confirmed)
        {
            return DomainResult.Failure(new("appointment.confirmed", "A consulta já está confirmada."));
        }

        if (Slot.StartUtc <= utcNow)
        {
            return DomainResult.Failure(new("appointment.slot.past", "Não é permitido confirmar uma consulta que já iniciou."));
        }

        Status = AppointmentStatus.Confirmed;
        AddDomainEvent(new AppointmentConfirmedDomainEvent(Id, PatientId, DoctorId, Slot.StartUtc, utcNow));
        return DomainResult.Success();
    }

    public DomainResult Reschedule(AppointmentSlot slot, DateTime utcNow)
    {
        if (Status == AppointmentStatus.Cancelled)
        {
            return DomainResult.Failure(new("appointment.cancelled", "Uma consulta cancelada não pode ser reagendada."));
        }

        if (slot.StartUtc <= utcNow)
        {
            return DomainResult.Failure(new("appointment.slot.past", "Não é permitido reagendar para um horário passado."));
        }

        Slot = slot;
        Status = AppointmentStatus.Scheduled;
        return DomainResult.Success();
    }

    public DomainResult Cancel(string? reason)
    {
        var normalizedReason = reason?.Trim();

        if (Status == AppointmentStatus.Cancelled)
        {
            return DomainResult.Failure(new("appointment.cancelled", "A consulta já está cancelada."));
        }

        if (string.IsNullOrWhiteSpace(normalizedReason) || normalizedReason.Length > 500)
        {
            return DomainResult.Failure(new("appointment.cancellation_reason.invalid", "O motivo do cancelamento é obrigatório e deve ter até 500 caracteres."));
        }

        Status = AppointmentStatus.Cancelled;
        CancellationReason = normalizedReason;
        return DomainResult.Success();
    }
}
