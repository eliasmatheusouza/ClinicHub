using ClinicHub.Application.Common;
using ClinicHub.Application.Users.Dtos;

namespace ClinicHub.Application.Users.Queries.GetDoctors;

public sealed record GetDoctorsQuery : IQuery<ApplicationResult<IReadOnlyCollection<DoctorOptionDto>>>;
