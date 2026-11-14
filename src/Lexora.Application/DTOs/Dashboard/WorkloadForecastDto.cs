namespace Lexora.Application.DTOs;

public record DailyForecastItemDto(
    DateOnly Date,
    string DayLabel,
    int DueCount,
    bool IsToday,
    string Intensity
)
{
    public string DisplayDate => Date.ToString("dd/MM");
}

public record WorkloadForecastDto(
    IReadOnlyList<DailyForecastItemDto> Days,
    int TotalDueNext7Days,
    int PeakDayDueCount,
    string PeakDayLabel
);
