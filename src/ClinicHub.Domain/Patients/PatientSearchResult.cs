namespace ClinicHub.Domain.Patients;

public sealed record PatientSearchResult(IReadOnlyCollection<Patient> Items, int TotalCount);
