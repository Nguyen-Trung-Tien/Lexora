namespace Lexora.Application.DTOs;

public record UserPreferences
{
    public int ThemeIndex { get; init; } = 0; // 0 = System, 1 = Light, 2 = Dark
    public string LanguageCode { get; init; } = "vi-VN";
    public bool AutoPlayAudio { get; init; } = false;
    public bool EnableReviewReminder { get; init; } = true;
    public double SpeechRate { get; init; } = 1.0;
    public bool EnableTypingPractice { get; init; } = false;
    public bool IsNavPaneOpen { get; init; } = true;
    public string PreferredAccent { get; init; } = "uk"; // "uk" (en-GB) or "us" (en-US)
    public bool AutoCheckUpdates { get; init; } = true;
    public string SpacedRepetitionAlgorithm { get; init; } = "SM2"; // "SM2" or "FSRS"
    public double TargetRetention { get; init; } = 0.90; // 0.85, 0.90, 0.95

    // Window state — restored on next launch
    public int WindowWidth { get; init; } = 1280;
    public int WindowHeight { get; init; } = 768;
    public int WindowX { get; init; } = -1;  // -1 means "center on screen"
    public int WindowY { get; init; } = -1;
    public bool IsMaximized { get; init; } = false;
}
