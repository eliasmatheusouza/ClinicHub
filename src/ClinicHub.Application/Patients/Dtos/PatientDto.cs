using ClinicHub.Domain.Patients;

namespace ClinicHub.Application.Patients.Dtos;

public sealed record PatientDto(
    Guid Id,
    string Name,
    DateOnly BirthDate,
    string Email,
    string Phone,
    bool IsActive)
{
    public static PatientDto FromDomain(Patient patient) => new(
        patient.Id,
        patient.Name.Value,
        patient.BirthDate,
        patient.Email.Value,
        patient.Phone.Value,
        patient.IsActive);
}
