using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Appointments.Dtos;
using ClinicHub.Application.Common;
using ClinicHub.Domain.Appointments;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Users;
using ClinicHub.Domain.ValueObjects;
using MediatR;

namespace ClinicHub.Application.Appointments.Commands.ScheduleAppointment;

public sealed class ScheduleAppointmentCommandHandler(
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<ScheduleAppointmentCommand, ApplicationResult<AppointmentDto>>
{
    public async Task<ApplicationResult<AppointmentDto>> Handle(ScheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null || !patient.IsActive)
        {
            return ApplicationResult<AppointmentDto>.Failure(new("patient.not_found", "Paciente não encontrado."));
        }

        var doctor = await userRepository.GetByIdAsync(request.DoctorId, cancellationToken);
        if (doctor is null || !doctor.IsActive || doctor.Role != UserRole.Doctor)
        {
            return ApplicationResult<AppointmentDto>.Failure(new("doctor.not_found", "Médico não encontrado."));
        }

        var slotResult = AppointmentSlot.Create(request.StartUtc, TimeSpan.FromMinutes(request.DurationMinutes));
        if (!slotResult.IsSuccess)
        {
            return ApplicationResult<AppointmentDto>.FailureFromDomain(slotResult.Notifications);
        }

        if (await appointmentRepository.HasConflictAsync(request.DoctorId, slotResult.Value!, cancellationToken: cancellationToken))
        {
            return ApplicationResult<AppointmentDto>.Failure(new("appointment.conflict", "O médico já possui uma consulta nesse intervalo."));
        }

        var appointmentResult = Appointment.Create(Guid.NewGuid(), request.PatientId, request.DoctorId, slotResult.Value!, clock.UtcNow);
        if (!appointmentResult.IsSuccess)
        {
            return ApplicationResult<AppointmentDto>.FailureFromDomain(appointmentResult.Notifications);
        }

        await appointmentRepository.AddAsync(appointmentResult.Value!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<AppointmentDto>.Success(AppointmentDto.FromDomain(appointmentResult.Value!));
    }
}
