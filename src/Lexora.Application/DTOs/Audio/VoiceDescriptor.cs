namespace Lexora.Application.DTOs;

public record VoiceDescriptor(
    string Id,
    string DisplayName,
    string Language, // e.g. "en-US", "en-GB"
    string Gender,
    bool IsDefault = false
);

public record VoiceResolutionResult(
    string RequestedLocale, // Normalized: "en-US" or "en-GB"
    VoiceDescriptor? Voice,
    bool IsExactMatch,
    bool IsFallback,
    string? FallbackReason
);
