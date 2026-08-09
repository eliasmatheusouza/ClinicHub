using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClinicHub.Infrastructure.Persistence;

public sealed class ClinicHubDbContextFactory : IDesignTimeDbContextFactory<ClinicHubDbContext>
{
    public ClinicHubDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
            ?? "Server=localhost,1433;Database=ClinicHub;User Id=sa;Password=ClinicHub_dev_2026!;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ClinicHubDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ClinicHubDbContext(options);
    }
}
