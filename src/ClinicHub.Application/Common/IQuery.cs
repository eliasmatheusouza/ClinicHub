using MediatR;

namespace ClinicHub.Application.Common;

public interface IQuery<out TResponse> : IRequest<TResponse>;
