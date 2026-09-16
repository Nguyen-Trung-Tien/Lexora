using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Services;

public record UserPreferences
{
    public int ThemeIndex { get; init; } = 0; // 0 = System, 1 = Light, 2 = Dark
    public string LanguageCode { get; init; } = "vi-VN";
    public bool AutoPlayAudio { get; init; } = false;
    public bool EnableReviewReminder { get; init; } = true;
    public double SpeechRate { get; init; } = 1.0;
    public bool EnableTypingPractice { get; init; } = false;
    public bool IsNavPaneOpen { get; init; } = true;
}

public interface IUserPreferencesService
{
    UserPreferences GetPreferences();
    Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    event EventHandler<UserPreferences>? PreferencesChanged;
}

public class UserPreferencesService : IUserPreferencesService
{
    private readonly string _settingsFilePath;
    private readonly ILogger<UserPreferencesService> _logger;
    private readonly object _lock = new();
    private UserPreferences _currentPreferences;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public event EventHandler<UserPreferences>? PreferencesChanged;

    public UserPreferencesService(ILogger<UserPreferencesService> logger, string? basePath = null)
    {
        _logger = logger;
        var folder = basePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Lexora");

        Directory.CreateDirectory(folder);
        _settingsFilePath = Path.Combine(folder, "settings.json");
        _currentPreferences = LoadPreferencesFromFile();
    }

    public UserPreferences GetPreferences()
    {
        lock (_lock)
        {
            return _currentPreferences;
        }
    }

    public async Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _currentPreferences = preferences;
        }

        try
        {
            var json = JsonSerializer.Serialize(preferences, JsonOptions);
            var tempPath = _settingsFilePath + ".tmp";

            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, _settingsFilePath, overwrite: true);

            _logger.LogInformation("Saved user preferences: Theme={Theme}, Language={Lang}", preferences.ThemeIndex, preferences.LanguageCode);
            PreferencesChanged?.Invoke(this, preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user preferences to {Path}", _settingsFilePath);
        }
    }

    private UserPreferences LoadPreferencesFromFile()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<UserPreferences>(json, JsonOptions);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load preferences from {Path}, using defaults.", _settingsFilePath);
        }

        return new UserPreferences();
    }
}
