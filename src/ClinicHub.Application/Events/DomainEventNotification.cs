using ClinicHub.Domain.Common;
using MediatR;

namespace ClinicHub.Application.Events;

public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
