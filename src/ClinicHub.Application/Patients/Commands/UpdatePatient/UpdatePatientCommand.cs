using ClinicHub.Application.Common;
using ClinicHub.Application.Patients.Dtos;

namespace ClinicHub.Application.Patients.Commands.UpdatePatient;

public sealed record UpdatePatientCommand(Guid PatientId, string Name, DateOnly BirthDate, string Email, string Phone) : ICommand<ApplicationResult<PatientDto>>;
