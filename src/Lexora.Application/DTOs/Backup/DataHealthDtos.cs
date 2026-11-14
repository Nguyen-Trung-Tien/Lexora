namespace Lexora.Application.DTOs;

public enum DatabaseHealthRating
{
    Optimal = 0,
    Good = 1,
    NeedsMaintenance = 2,
    Critical = 3
}

public record DatabaseHealthReportDto(
    string SqliteIntegrityStatus,
    string QuickCheckStatus,
    long DatabaseSizeBytes,
    int PageCount,
    int PageSize,
    int FreelistCount,
    double FragmentationPercent,
    long WalFileSizeBytes,
    int OrphanMeaningsCount,
    int OrphanExamplesCount,
    int OrphanStudyLogsCount,
    int InvalidReviewCardsCount,
    int MissingReviewCardsCount,
    int DuplicateTermsCount,
    int AudioCacheFilesCount,
    long AudioCacheSizeBytes,
    int HealthScore,
    DatabaseHealthRating HealthRating,
    DateTime CheckedAt
)
{
    public string FormattedDatabaseSize => FormatBytes(DatabaseSizeBytes);
    public string FormattedWalSize => FormatBytes(WalFileSizeBytes);
    public string FormattedAudioCacheSize => FormatBytes(AudioCacheSizeBytes);
    public string FormattedReclaimableSpace => FormatBytes((long)FreelistCount * PageSize);

    public bool HasIssues =>
        HealthRating >= DatabaseHealthRating.NeedsMaintenance ||
        OrphanMeaningsCount > 0 ||
        OrphanExamplesCount > 0 ||
        InvalidReviewCardsCount > 0 ||
        MissingReviewCardsCount > 0;

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 KB";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F2} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}

public record DataRepairResultDto(
    int RepairedOrphansCount,
    int ReconciledCardsCount,
    int FixedIntervalsCount,
    long BytesReclaimed,
    long OptimizationDurationMs,
    bool Success,
    string Summary,
    DateTime CompletedAt
);
