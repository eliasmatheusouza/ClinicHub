using FluentValidation;
using MediatR;

namespace ClinicHub.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IApplicationResult, new()
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validationContext = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(validationContext, cancellationToken)));
        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => new ApplicationError($"validation.{failure.PropertyName}", failure.ErrorMessage))
            .ToArray();

        if (errors.Length == 0)
        {
            return await next();
        }

        var response = new TResponse();
        response.AddErrors(errors);
        return response;
    }
}
