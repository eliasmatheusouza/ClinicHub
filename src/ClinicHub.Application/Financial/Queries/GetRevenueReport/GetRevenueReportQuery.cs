using ClinicHub.Application.Common;
using ClinicHub.Application.Financial.Dtos;

namespace ClinicHub.Application.Financial.Queries.GetRevenueReport;

public sealed record GetRevenueReportQuery(DateOnly StartDate, DateOnly EndDate) : IQuery<ApplicationResult<RevenueReportDto>>;
