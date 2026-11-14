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
    public double BarHeight => DueCount == 0 ? 4.0 : Math.Clamp(DueCount * 3.5, 10.0, 84.0);
}

public record WorkloadForecastDto(
    IReadOnlyList<DailyForecastItemDto> Days,
    int TotalDueNext7Days,
    int PeakDayDueCount,
    string PeakDayLabel
);
