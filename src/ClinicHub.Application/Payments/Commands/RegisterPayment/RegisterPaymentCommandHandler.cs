using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Common;
using ClinicHub.Application.Payments.Dtos;
using ClinicHub.Domain.Appointments;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Payments;
using ClinicHub.Domain.ValueObjects;
using MediatR;

namespace ClinicHub.Application.Payments.Commands.RegisterPayment;

public sealed class RegisterPaymentCommandHandler(
    IAppointmentRepository appointmentRepository,
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<RegisterPaymentCommand, ApplicationResult<PaymentDto>>
{
    public async Task<ApplicationResult<PaymentDto>> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return ApplicationResult<PaymentDto>.Failure(new("appointment.not_found", "Consulta não encontrada."));
        }

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            return ApplicationResult<PaymentDto>.Failure(new("payment.appointment_not_confirmed", "O pagamento só pode ser registrado para uma consulta confirmada."));
        }

        if (await paymentRepository.ExistsForAppointmentAsync(request.AppointmentId, cancellationToken))
        {
            return ApplicationResult<PaymentDto>.Failure(new("payment.already_registered", "Já existe um pagamento registrado para esta consulta."));
        }

        var amountResult = Money.Create(request.Amount, request.Currency);
        if (!amountResult.IsSuccess)
        {
            return ApplicationResult<PaymentDto>.FailureFromDomain(amountResult.Notifications);
        }

        var paymentResult = Payment.Create(Guid.NewGuid(), request.AppointmentId, amountResult.Value!, request.Method, clock.UtcNow, clock.UtcNow);
        if (!paymentResult.IsSuccess)
        {
            return ApplicationResult<PaymentDto>.FailureFromDomain(paymentResult.Notifications);
        }

        await paymentRepository.AddAsync(paymentResult.Value!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<PaymentDto>.Success(PaymentDto.FromDomain(paymentResult.Value!));
    }
}
