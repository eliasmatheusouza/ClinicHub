namespace ClinicHub.Application.Common;

public interface IApplicationResult
{
    bool IsSuccess { get; }
    void AddErrors(IEnumerable<ApplicationError> errors);
}
