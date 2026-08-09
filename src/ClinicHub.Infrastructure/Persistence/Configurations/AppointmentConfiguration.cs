using ClinicHub.Domain.Appointments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicHub.Infrastructure.Persistence.Configurations;

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(appointment => appointment.Id);
        builder.Property(appointment => appointment.Id).ValueGeneratedNever();
        builder.Property(appointment => appointment.PatientId).IsRequired();
        builder.Property(appointment => appointment.DoctorId).IsRequired();
        builder.Property(appointment => appointment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(appointment => appointment.CancellationReason).HasMaxLength(500);
        builder.Ignore(appointment => appointment.DomainEvents);

        builder.OwnsOne(appointment => appointment.Slot, slot =>
        {
            slot.Property(valueObject => valueObject.StartUtc).HasColumnName("StartUtc").HasColumnType("datetime2").IsRequired();
            slot.Property(valueObject => valueObject.EndUtc).HasColumnName("EndUtc").HasColumnType("datetime2").IsRequired();
            slot.Property(valueObject => valueObject.Duration).HasColumnName("Duration").IsRequired();
            slot.HasIndex(valueObject => valueObject.StartUtc);
        });

        builder.HasIndex(appointment => new { appointment.DoctorId, appointment.Status });
    }
}
