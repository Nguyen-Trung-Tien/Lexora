using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public enum SmartImportField
{
    Ignore = 0,
    Term = 1,
    Level = 2,
    PartOfSpeech = 3,
    DefinitionVi = 4,
    DefinitionEn = 5,
    PhoneticUk = 6,
    PhoneticUs = 7,
    Examples = 8,
    Topics = 9,
    ExamTags = 10,
    Notes = 11
}

public record SmartImportColumnMappingDto(
    string DetectedHeader,
    SmartImportField TargetField
);

public record SmartImportItemDto(
    int RowIndex,
    string Term,
    CefrLevel Level,
    PartOfSpeech PartOfSpeech,
    string DefinitionVi,
    string? DefinitionEn,
    string? PhoneticUk,
    string? PhoneticUs,
    string? Examples,
    string? Topics,
    string? ExamTags,
    string? Notes,
    bool IsDuplicate,
    bool IsValid,
    string? ValidationMessage,
    bool IsSelected = true
);

public record SmartImportAnalysisResultDto(
    DataFormat DetectedFormat,
    IReadOnlyList<string> AvailableHeaders,
    IReadOnlyList<SmartImportColumnMappingDto> ColumnMappings,
    IReadOnlyList<SmartImportItemDto> PreviewItems,
    int TotalParsed,
    int ValidCount,
    int DuplicateCount,
    int InvalidCount
);

public record SmartImportCommitRequestDto(
    IReadOnlyList<SmartImportItemDto> Items,
    ImportConflictResolution ConflictResolution
);

public record SmartImportCommitResultDto(
    int TotalImported,
    int TotalUpdated,
    int TotalSkipped,
    IReadOnlyList<string> Errors
)
{
    public int TotalErrors => Errors?.Count ?? 0;
}
