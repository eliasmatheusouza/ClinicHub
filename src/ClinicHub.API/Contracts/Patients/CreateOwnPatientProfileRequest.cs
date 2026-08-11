namespace ClinicHub.API.Contracts.Patients;

public sealed record CreateOwnPatientProfileRequest(string Name, DateOnly BirthDate, string Phone);
