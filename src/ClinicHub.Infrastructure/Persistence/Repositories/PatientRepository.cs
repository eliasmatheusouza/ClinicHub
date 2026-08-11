using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.Patients;
using ClinicHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Infrastructure.Persistence.Repositories;

internal sealed class PatientRepository(ClinicHubDbContext context) : IPatientRepository
{
    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Patients.SingleOrDefaultAsync(patient => patient.Id == id, cancellationToken);

    public Task<Patient?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Patients.SingleOrDefaultAsync(patient => patient.UserId == userId, cancellationToken);

    public Task<bool> ExistsByEmailAsync(EmailAddress email, Guid? ignoredPatientId = null, CancellationToken cancellationToken = default) =>
        context.Patients.AnyAsync(patient => patient.Email.Value == email.Value && (!ignoredPatientId.HasValue || patient.Id != ignoredPatientId.Value), cancellationToken);

    public async Task<PatientSearchResult> SearchAsync(PatientSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var query = context.Patients.AsNoTracking().Where(patient => patient.IsActive);
        if (!string.IsNullOrWhiteSpace(filter.Term))
        {
            var term = filter.Term.Trim();
            query = query.Where(patient => EF.Functions.Like(patient.Name.Value, $"%{term}%") || EF.Functions.Like(patient.Email.Value, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(patient => patient.Name.Value)
            .ThenBy(patient => patient.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new(items, totalCount);
    }

    public Task AddAsync(Patient patient, CancellationToken cancellationToken = default) =>
        context.Patients.AddAsync(patient, cancellationToken).AsTask();

    public void Update(Patient patient) => context.Patients.Update(patient);
}
