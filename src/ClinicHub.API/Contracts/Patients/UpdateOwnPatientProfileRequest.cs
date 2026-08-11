namespace ClinicHub.API.Contracts.Patients;

public sealed record UpdateOwnPatientProfileRequest(string Name, DateOnly BirthDate, string Phone);
