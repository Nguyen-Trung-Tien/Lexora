using System.Text.Json.Serialization;

namespace Lexora.Application.DTOs;

public enum CommunityPackageStatus
{
    NotInstalled = 0,
    Downloading = 1,
    Installing = 2,
    Installed = 3,
    UpdateAvailable = 4,
    Error = 5
}

public record CommunityPackageMetadataDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = "Community";
    public string Category { get; init; } = "Vocabulary"; // "Vocabulary" or "Grammar"
    public string TargetExam { get; init; } = "General"; // "TOEIC", "IELTS", "VSTEP", "General"
    public string Level { get; init; } = "All"; // "A1", "A2", "B1", "B2", "C1", "C2", "All"
    public string Version { get; init; } = "1.0.0";
    public int ItemCount { get; init; } = 0;
    public string Language { get; init; } = "en-US";
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string DownloadUrl { get; init; } = string.Empty;
    public string? ChecksumSha256 { get; init; }
    public string License { get; init; } = "CC-BY-4.0";
    public long SizeInBytes { get; init; } = 0;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public record CommunityPackagePayloadDto
{
    public string FormatVersion { get; init; } = "1.0";
    public CommunityPackageMetadataDto Metadata { get; init; } = new();
    public List<WordExportModel>? VocabularyWords { get; init; }
    public List<GrammarRuleExportModel>? GrammarRules { get; init; }
}

public record CommunityCatalogDto
{
    public string CatalogVersion { get; init; } = "1.0";
    public string Repository { get; init; } = string.Empty;
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<CommunityPackageMetadataDto> Packages { get; init; } = [];
}

public record InstalledPackageRecord
{
    public string PackageId { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public DateTime InstalledAt { get; init; } = DateTime.UtcNow;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "Vocabulary";
    public int ItemCount { get; init; } = 0;
    public string LocalSource { get; init; } = "Community";
    public string InstalledAtFormatted => InstalledAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

public record CommunityPackageFilterDto(
    string? Category = null,
    string? SearchQuery = null,
    string? Level = null,
    string? Exam = null);
