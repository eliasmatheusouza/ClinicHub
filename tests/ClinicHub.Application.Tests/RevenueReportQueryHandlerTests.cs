using ClinicHub.Application.Financial.Abstractions;
using ClinicHub.Application.Financial.Dtos;
using ClinicHub.Application.Financial.Queries.GetRevenueReport;
using Moq;

namespace ClinicHub.Application.Tests;

public sealed class RevenueReportQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsReaderProjectionForRequestedPeriod()
    {
        var reader = new Mock<IRevenueReportReader>();
        reader.Setup(value => value.GetByPeriodAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DailyRevenueDto(new DateOnly(2026, 8, 1), "BRL", 320m, 2)]);
        var handler = new GetRevenueReportQueryHandler(reader.Object);

        var result = await handler.Handle(new(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(320m, result.Value.Items.Single().GrossRevenue);
    }
}
