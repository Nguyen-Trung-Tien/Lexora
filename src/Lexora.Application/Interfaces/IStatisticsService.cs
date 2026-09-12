using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IStatisticsService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
}
