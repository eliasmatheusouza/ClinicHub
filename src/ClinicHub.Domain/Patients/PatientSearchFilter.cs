namespace ClinicHub.Domain.Patients;

public sealed record PatientSearchFilter(string? Term, int Page, int PageSize);
