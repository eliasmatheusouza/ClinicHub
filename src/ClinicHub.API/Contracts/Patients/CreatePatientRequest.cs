namespace ClinicHub.API.Contracts.Patients;

public sealed record CreatePatientRequest(string Name, DateOnly BirthDate, string Email, string Phone);
