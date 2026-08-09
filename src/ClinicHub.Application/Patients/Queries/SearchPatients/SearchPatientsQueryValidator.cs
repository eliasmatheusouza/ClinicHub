using FluentValidation;

namespace ClinicHub.Application.Patients.Queries.SearchPatients;

public sealed class SearchPatientsQueryValidator : AbstractValidator<SearchPatientsQuery>
{
    public SearchPatientsQueryValidator()
    {
        RuleFor(query => query.Term).MaximumLength(100).When(query => query.Term is not null);
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
