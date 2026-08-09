using FluentValidation;

namespace ClinicHub.Application.Financial.Queries.GetRevenueReport;

public sealed class GetRevenueReportQueryValidator : AbstractValidator<GetRevenueReportQuery>
{
    public GetRevenueReportQueryValidator()
    {
        RuleFor(query => query.StartDate).NotEqual(default(DateOnly));
        RuleFor(query => query.EndDate).GreaterThanOrEqualTo(query => query.StartDate);
    }
}
