using ClinicHub.Domain.Patients;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(EmailAddress email, Guid? ignoredPatientId = null, CancellationToken cancellationToken = default);
    Task<PatientSearchResult> SearchAsync(PatientSearchFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    void Update(Patient patient);
}
