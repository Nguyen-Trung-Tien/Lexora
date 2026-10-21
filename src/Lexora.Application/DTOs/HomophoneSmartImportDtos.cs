using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public record HomophoneImportExportItemDto
{
    public string Term { get; init; } = string.Empty;
    public string PartOfSpeech { get; init; } = "Noun";
    public string MeaningVi { get; init; } = string.Empty;
    public string? MeaningEn { get; init; }
    public string? PhoneticSpecific { get; init; }
    public string? ExampleSentence { get; init; }
    public string? ExampleTranslation { get; init; }
}

public record HomophoneImportExportGroupDto
{
    public string Title { get; init; } = string.Empty;
    public string PhoneticIpa { get; init; } = string.Empty;
    public string Type { get; init; } = "ExactHomophone";
    public string Level { get; init; } = "None";
    public string ExamTag { get; init; } = "None";
    public string? DifferentiationTip { get; init; }
    public string? Notes { get; init; }
    public List<HomophoneImportExportItemDto> Items { get; init; } = [];
}

public record SmartHomophonePreviewItemDto
{
    public int RowNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string PhoneticIpa { get; init; } = string.Empty;
    public string Type { get; init; } = "ExactHomophone";
    public string Level { get; init; } = "None";
    public string? DifferentiationTip { get; init; }
    public int ItemCount { get; init; }
    public string ItemsSummary { get; init; } = string.Empty;
    public bool IsDuplicate { get; init; }
    public bool IsValid { get; init; }
    public string? ValidationMessage { get; init; }
    public HomophoneImportExportGroupDto RawGroup { get; init; } = new();
}

public record SmartHomophoneImportAnalysisDto
{
    public int TotalGroupsFound { get; init; }
    public int ValidGroupsCount { get; init; }
    public int DuplicateGroupsCount { get; init; }
    public int TotalWordsCount { get; init; }
    public DataFormat DetectedFormat { get; init; }
    public IReadOnlyList<SmartHomophonePreviewItemDto> PreviewItems { get; init; } = [];
    public IReadOnlyList<string> ParseErrors { get; init; } = [];
}

public record SmartHomophoneCommitRequestDto
{
    public IReadOnlyList<HomophoneImportExportGroupDto> GroupsToCommit { get; init; } = [];
    public ImportConflictResolution ConflictResolution { get; init; } = ImportConflictResolution.Skip;
}
