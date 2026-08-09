namespace ClinicHub.Domain.Common;

public class DomainResult
{
    protected DomainResult(IReadOnlyCollection<DomainNotification> notifications)
    {
        Notifications = notifications;
    }

    public bool IsSuccess => Notifications.Count == 0;
    public IReadOnlyCollection<DomainNotification> Notifications { get; }

    public static DomainResult Success() => new(Array.Empty<DomainNotification>());

    public static DomainResult Failure(DomainNotification notification) => new([notification]);

}

public sealed class DomainResult<T> : DomainResult
{
    private DomainResult(T value) : base(Array.Empty<DomainNotification>())
    {
        Value = value;
    }

    private DomainResult(IReadOnlyCollection<DomainNotification> notifications) : base(notifications)
    {
    }

    public T? Value { get; }

    public static DomainResult<T> Success(T value) => new(value);

    public new static DomainResult<T> Failure(DomainNotification notification) => new([notification]);

}
