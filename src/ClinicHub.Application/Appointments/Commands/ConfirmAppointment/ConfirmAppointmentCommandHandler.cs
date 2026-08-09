using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Appointments.Dtos;
using ClinicHub.Application.Common;
using ClinicHub.Application.Events;
using ClinicHub.Domain.Interfaces;
using MediatR;

namespace ClinicHub.Application.Appointments.Commands.ConfirmAppointment;

public sealed class ConfirmAppointmentCommandHandler(
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher,
    IClock clock) : IRequestHandler<ConfirmAppointmentCommand, ApplicationResult<AppointmentDto>>
{
    public async Task<ApplicationResult<AppointmentDto>> Handle(ConfirmAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return ApplicationResult<AppointmentDto>.Failure(new("appointment.not_found", "Consulta não encontrada."));
        }

        var confirmationResult = appointment.Confirm(clock.UtcNow);
        if (!confirmationResult.IsSuccess)
        {
            return ApplicationResult<AppointmentDto>.FailureFromDomain(confirmationResult.Notifications);
        }

        appointmentRepository.Update(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await domainEventDispatcher.DispatchAsync(appointment.DomainEvents, cancellationToken);
        appointment.ClearDomainEvents();

        return ApplicationResult<AppointmentDto>.Success(AppointmentDto.FromDomain(appointment));
    }
}
