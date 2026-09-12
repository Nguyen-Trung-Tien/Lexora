using FluentAssertions;
using Lexora.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Unit;

public class UserPreferencesServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsFilePath;

    public UserPreferencesServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Lexora_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _settingsFilePath = Path.Combine(_tempDirectory, "settings.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup exceptions in tests
        }
    }

    [Fact]
    public void LoadPreferences_WhenFileDoesNotExist_ShouldReturnDefaultSettings()
    {
        // Arrange
        var service = new UserPreferencesService(NullLogger<UserPreferencesService>.Instance, _tempDirectory);

        // Act
        var prefs = service.GetPreferences();

        // Assert
        prefs.Should().NotBeNull();
        prefs.ThemeIndex.Should().Be(0);
        prefs.LanguageCode.Should().Be("vi-VN");
        prefs.AutoPlayAudio.Should().BeFalse();
        prefs.EnableReviewReminder.Should().BeTrue();
        prefs.SpeechRate.Should().Be(1.0);
        prefs.EnableTypingPractice.Should().BeFalse();
    }

    [Fact]
    public async Task SavePreferencesAsync_ShouldPersistToFileAndRaiseEvent()
    {
        // Arrange
        var service = new UserPreferencesService(NullLogger<UserPreferencesService>.Instance, _tempDirectory);
        UserPreferences? receivedPrefs = null;
        service.PreferencesChanged += (_, p) => receivedPrefs = p;

        var newPrefs = new UserPreferences
        {
            ThemeIndex = 2,
            LanguageCode = "en-US",
            AutoPlayAudio = true,
            EnableReviewReminder = false,
            SpeechRate = 0.75,
            EnableTypingPractice = true
        };

        // Act
        await service.SavePreferencesAsync(newPrefs);

        // Assert
        service.GetPreferences().Should().BeEquivalentTo(newPrefs);
        receivedPrefs.Should().BeEquivalentTo(newPrefs);
        File.Exists(_settingsFilePath).Should().BeTrue();

        // Reload fresh from disk
        var reloaded = new UserPreferencesService(NullLogger<UserPreferencesService>.Instance, _tempDirectory);
        reloaded.GetPreferences().Should().BeEquivalentTo(newPrefs);
    }
}
