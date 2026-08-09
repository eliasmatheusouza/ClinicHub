using ClinicHub.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicHub.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever();
        builder.Property(payment => payment.AppointmentId).IsRequired();
        builder.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.PaidAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Ignore(payment => payment.DomainEvents);

        builder.OwnsOne(payment => payment.Amount, money =>
        {
            money.Property(valueObject => valueObject.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            money.Property(valueObject => valueObject.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(payment => payment.AppointmentId).IsUnique();
        builder.HasIndex(payment => payment.PaidAtUtc);
    }
}
