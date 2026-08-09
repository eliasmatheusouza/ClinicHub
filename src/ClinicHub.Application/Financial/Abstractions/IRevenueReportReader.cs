using ClinicHub.Application.Financial.Dtos;

namespace ClinicHub.Application.Financial.Abstractions;

public interface IRevenueReportReader
{
    Task<IReadOnlyCollection<DailyRevenueDto>> GetByPeriodAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
