using ClinicHub.Application.Appointments.Dtos;
using ClinicHub.Application.Common;
using ClinicHub.Domain.Interfaces;
using MediatR;

namespace ClinicHub.Application.Appointments.Commands.CancelAppointment;

public sealed class CancelAppointmentCommandHandler(IAppointmentRepository appointmentRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CancelAppointmentCommand, ApplicationResult<AppointmentDto>>
{
    public async Task<ApplicationResult<AppointmentDto>> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return ApplicationResult<AppointmentDto>.Failure(new("appointment.not_found", "Consulta não encontrada."));
        }

        var cancellationResult = appointment.Cancel(request.Reason);
        if (!cancellationResult.IsSuccess)
        {
            return ApplicationResult<AppointmentDto>.FailureFromDomain(cancellationResult.Notifications);
        }

        appointmentRepository.Update(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<AppointmentDto>.Success(AppointmentDto.FromDomain(appointment));
    }
}
