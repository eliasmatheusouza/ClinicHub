using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;

namespace ClinicHub.Application.Patients.Commands.CreatePatient;

public sealed record CreatePatientCommand(
    string Name,
    DateOnly BirthDate,
    string Email,
    string Phone) : ICommand<ApplicationResult<PatientDto>>;
