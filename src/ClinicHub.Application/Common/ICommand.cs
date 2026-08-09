using MediatR;

namespace ClinicHub.Application.Common;

public interface ICommand<out TResponse> : IRequest<TResponse>;
