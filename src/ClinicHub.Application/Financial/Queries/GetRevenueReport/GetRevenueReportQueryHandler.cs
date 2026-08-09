using ClinicHub.Application.Common;
using ClinicHub.Application.Financial.Abstractions;
using ClinicHub.Application.Financial.Dtos;
using MediatR;

namespace ClinicHub.Application.Financial.Queries.GetRevenueReport;

public sealed class GetRevenueReportQueryHandler(IRevenueReportReader revenueReportReader)
    : IRequestHandler<GetRevenueReportQuery, ApplicationResult<RevenueReportDto>>
{
    public async Task<ApplicationResult<RevenueReportDto>> Handle(GetRevenueReportQuery request, CancellationToken cancellationToken)
    {
        var items = await revenueReportReader.GetByPeriodAsync(request.StartDate, request.EndDate, cancellationToken);
        return ApplicationResult<RevenueReportDto>.Success(new RevenueReportDto(request.StartDate, request.EndDate, items));
    }
}
