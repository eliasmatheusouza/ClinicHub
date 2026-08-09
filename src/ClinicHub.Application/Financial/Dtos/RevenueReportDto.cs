namespace ClinicHub.Application.Financial.Dtos;

public sealed record RevenueReportDto(DateOnly StartDate, DateOnly EndDate, IReadOnlyCollection<DailyRevenueDto> Items);
