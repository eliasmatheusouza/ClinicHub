using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;

namespace ClinicHub.Application.Patients.Commands.CreateOwnPatientProfile;

public sealed record CreateOwnPatientProfileCommand(Guid UserId, string Name, DateOnly BirthDate, string Phone)
    : ICommand<ApplicationResult<PatientDto>>;
