namespace Lexora.Application.DTOs;

public record BackupMetadata(
    string FilePath,
    string FileName,
    long FileSizeBytes,
    string FileSizeFormatted,
    DateTime LastModified,
    int WordCount,
    int MeaningCount,
    int GrammarRuleCount,
    int StudyLogCount,
    bool IsValid,
    string? ErrorMessage = null);
