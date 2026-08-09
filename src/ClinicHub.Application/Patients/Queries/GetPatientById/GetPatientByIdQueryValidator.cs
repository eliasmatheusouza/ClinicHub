using FluentValidation;

namespace ClinicHub.Application.Patients.Queries.GetPatientById;

public sealed class GetPatientByIdQueryValidator : AbstractValidator<GetPatientByIdQuery>
{
    public GetPatientByIdQueryValidator()
    {
        RuleFor(query => query.PatientId).NotEmpty();
    }
}
