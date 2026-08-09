namespace ClinicHub.Application.Financial.Dtos;

public sealed record DailyRevenueDto(DateOnly Date, string Currency, decimal GrossRevenue, int PaymentCount);
