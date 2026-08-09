using ClinicHub.Domain.Common;

namespace ClinicHub.Application.Common;

public sealed class ApplicationResult<T> : IApplicationResult
{
    private readonly List<ApplicationError> _errors = [];

    public bool IsSuccess => _errors.Count == 0;
    public T? Value { get; private set; }
    public IReadOnlyCollection<ApplicationError> Errors => _errors.AsReadOnly();

    public static ApplicationResult<T> Success(T value) => new() { Value = value };

    public static ApplicationResult<T> Failure(ApplicationError error)
    {
        var result = new ApplicationResult<T>();
        result.AddErrors([error]);
        return result;
    }

    public static ApplicationResult<T> FailureFromDomain(IEnumerable<DomainNotification> notifications)
    {
        var result = new ApplicationResult<T>();
        result.AddErrors(notifications.Select(notification => new ApplicationError(notification.Code, notification.Message)));
        return result;
    }

    public void AddErrors(IEnumerable<ApplicationError> errors) => _errors.AddRange(errors);
}
