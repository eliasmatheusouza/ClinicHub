using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Appointments.Dtos;
using ClinicHub.Application.Common;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.ValueObjects;
using MediatR;

namespace ClinicHub.Application.Appointments.Commands.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandHandler(
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<RescheduleAppointmentCommand, ApplicationResult<AppointmentDto>>
{
    public async Task<ApplicationResult<AppointmentDto>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return ApplicationResult<AppointmentDto>.Failure(new("appointment.not_found", "Consulta não encontrada."));
        }

        var slotResult = AppointmentSlot.Create(request.StartUtc, TimeSpan.FromMinutes(request.DurationMinutes));
        if (!slotResult.IsSuccess)
        {
            return ApplicationResult<AppointmentDto>.FailureFromDomain(slotResult.Notifications);
        }

        if (await appointmentRepository.HasConflictAsync(appointment.DoctorId, slotResult.Value!, appointment.Id, cancellationToken))
        {
            return ApplicationResult<AppointmentDto>.Failure(new("appointment.conflict", "O médico já possui uma consulta nesse intervalo."));
        }

        var rescheduleResult = appointment.Reschedule(slotResult.Value!, clock.UtcNow);
        if (!rescheduleResult.IsSuccess)
        {
            return ApplicationResult<AppointmentDto>.FailureFromDomain(rescheduleResult.Notifications);
        }

        appointmentRepository.Update(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<AppointmentDto>.Success(AppointmentDto.FromDomain(appointment));
    }
}
