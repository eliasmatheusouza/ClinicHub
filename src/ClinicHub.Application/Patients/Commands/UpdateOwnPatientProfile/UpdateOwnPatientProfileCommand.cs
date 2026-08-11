using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;

namespace ClinicHub.Application.Patients.Commands.UpdateOwnPatientProfile;

public sealed record UpdateOwnPatientProfileCommand(Guid UserId, string Name, DateOnly BirthDate, string Phone)
    : ICommand<ApplicationResult<PatientDto>>;
