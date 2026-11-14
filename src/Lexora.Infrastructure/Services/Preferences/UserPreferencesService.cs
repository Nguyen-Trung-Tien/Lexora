using System.Text.Json;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Services;

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

        var tempPath = $"{_settingsFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var json = JsonSerializer.Serialize(preferences, JsonOptions);
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, _settingsFilePath, overwrite: true);

            _logger.LogInformation("Saved user preferences: Theme={Theme}, Language={Lang}", preferences.ThemeIndex, preferences.LanguageCode);
            PreferencesChanged?.Invoke(this, preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user preferences to {Path}", _settingsFilePath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
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
