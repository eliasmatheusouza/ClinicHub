using ClinicHub.Application.Financial.Abstractions;
using ClinicHub.Application.Financial.Dtos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ClinicHub.Infrastructure.Financial;

internal sealed class DapperRevenueReportReader(IConfiguration configuration) : IRevenueReportReader
{
    private const string Query = """
        SELECT
            CAST(PaidAtUtc AS date) AS [Date],
            Currency,
            SUM(Amount) AS GrossRevenue,
            COUNT(*) AS PaymentCount
        FROM Payments
        WHERE PaidAtUtc >= @StartUtc AND PaidAtUtc < @EndExclusiveUtc
        GROUP BY CAST(PaidAtUtc AS date), Currency
        ORDER BY [Date], Currency;
        """;

    public async Task<IReadOnlyCollection<DailyRevenueDto>> GetByPeriodAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("A connection string 'SqlServer' deve ser configurada.");
        var startUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusiveUtc = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        await using var connection = new SqlConnection(connectionString);
        var rows = await connection.QueryAsync<RevenueRow>(new CommandDefinition(
            Query,
            new { StartUtc = startUtc, EndExclusiveUtc = endExclusiveUtc },
            cancellationToken: cancellationToken));

        return rows
            .Select(row => new DailyRevenueDto(DateOnly.FromDateTime(row.Date), row.Currency, row.GrossRevenue, row.PaymentCount))
            .ToArray();
    }

    private sealed class RevenueRow
    {
        public DateTime Date { get; init; }
        public string Currency { get; init; } = string.Empty;
        public decimal GrossRevenue { get; init; }
        public int PaymentCount { get; init; }
    }
}
