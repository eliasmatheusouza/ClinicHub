using ClinicHub.Domain.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicHub.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.EmailConfirmationTokenHash).HasMaxLength(64);
        builder.Property(user => user.EmailConfirmationExpiresAtUtc);
        builder.Property(user => user.EmailConfirmedAtUtc);
        builder.HasIndex(user => user.EmailConfirmationTokenHash).IsUnique().HasFilter("[EmailConfirmationTokenHash] IS NOT NULL");
        builder.Ignore(user => user.DomainEvents);

        builder.OwnsOne(user => user.Email, email =>
        {
            email.Property(valueObject => valueObject.Value).HasColumnName("Email").HasMaxLength(254).IsRequired();
            email.HasIndex(valueObject => valueObject.Value).IsUnique();
        });
    }
}
