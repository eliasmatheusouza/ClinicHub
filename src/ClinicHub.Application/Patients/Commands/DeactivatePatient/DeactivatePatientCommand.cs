using ClinicHub.Application.Common;

namespace ClinicHub.Application.Patients.Commands.DeactivatePatient;

public sealed record DeactivatePatientCommand(Guid PatientId) : ICommand<ApplicationResult<Guid>>;
