namespace ClinicHub.API.Contracts.Patients;

public sealed record UpdatePatientRequest(string Name, DateOnly BirthDate, string Email, string Phone);
