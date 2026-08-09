using ClinicHub.Domain.Authentication;

namespace ClinicHub.Application.Users.Dtos;

public sealed record DoctorOptionDto(Guid Id, string Email)
{
    public static DoctorOptionDto FromDomain(User user) => new(user.Id, user.Email.Value);
}
