using ClinicHub.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicHub.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(patient => patient.Id);
        builder.Property(patient => patient.Id).ValueGeneratedNever();
        builder.Property(patient => patient.BirthDate).HasColumnType("date").IsRequired();
        builder.Property(patient => patient.IsActive).IsRequired();
        builder.Property(patient => patient.UserId);
        builder.HasIndex(patient => patient.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        builder.Ignore(patient => patient.DomainEvents);

        builder.OwnsOne(patient => patient.Name, name =>
        {
            name.Property(valueObject => valueObject.Value).HasColumnName("Name").HasMaxLength(120).IsRequired();
        });

        builder.OwnsOne(patient => patient.Email, email =>
        {
            email.Property(valueObject => valueObject.Value).HasColumnName("Email").HasMaxLength(254).IsRequired();
            email.HasIndex(valueObject => valueObject.Value).IsUnique();
        });

        builder.OwnsOne(patient => patient.Phone, phone =>
        {
            phone.Property(valueObject => valueObject.Value).HasColumnName("Phone").HasMaxLength(15).IsRequired();
        });
    }
}
