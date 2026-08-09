using ClinicHub.Application.Common;
using ClinicHub.Application.Users.Dtos;
using ClinicHub.Domain.Interfaces;
using MediatR;

namespace ClinicHub.Application.Users.Queries.GetDoctors;

public sealed class GetDoctorsQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetDoctorsQuery, ApplicationResult<IReadOnlyCollection<DoctorOptionDto>>>
{
    public async Task<ApplicationResult<IReadOnlyCollection<DoctorOptionDto>>> Handle(GetDoctorsQuery request, CancellationToken cancellationToken)
    {
        var doctors = await userRepository.GetActiveDoctorsAsync(cancellationToken);
        return ApplicationResult<IReadOnlyCollection<DoctorOptionDto>>.Success(doctors.Select(DoctorOptionDto.FromDomain).ToArray());
    }
}
