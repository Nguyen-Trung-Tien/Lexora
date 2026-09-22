using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IStudyAdvisorService
{
    Task<StudyAdviceSummaryDto> GetDailyAdviceAsync(DashboardSummaryDto? existingSummary = null, CancellationToken cancellationToken = default);
    Task<WorkloadForecastDto> Get7DayForecastAsync(CancellationToken cancellationToken = default);
}
