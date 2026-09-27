using System.IO;
using System.Text;
using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class PronunciationAndVoiceTests : IDisposable
{
    private readonly string _tempDir;

    public PronunciationAndVoiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lexora_voice_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public void ResolveVoice_WhenBothNativeVoicesExist_ResolvesExactMatches()
    {
        // Arrange
        var mockProvider = new Mock<IVoiceProvider>();
        var voices = new List<VoiceDescriptor>
        {
            new("v-us", "Microsoft David", "en-US", "Male", IsDefault: true),
            new("v-uk", "Microsoft George", "en-GB", "Male", IsDefault: false)
        };
        mockProvider.Setup(p => p.GetAllVoices()).Returns(voices);

        var resolver = new PronunciationVoiceResolver(mockProvider.Object);

        // Act
        var resUk = resolver.ResolveVoice("uk");
        var resUs = resolver.ResolveVoice("us");

        // Assert
        resUk.IsExactMatch.Should().BeTrue();
        resUk.IsFallback.Should().BeFalse();
        resUk.Voice.Should().NotBeNull();
        resUk.Voice!.Id.Should().Be("v-uk");
        resUk.RequestedLocale.Should().Be("en-GB");

        resUs.IsExactMatch.Should().BeTrue();
        resUs.IsFallback.Should().BeFalse();
        resUs.Voice.Should().NotBeNull();
        resUs.Voice!.Id.Should().Be("v-us");
        resUs.RequestedLocale.Should().Be("en-US");
    }

    [Fact]
    public void ResolveVoice_WhenUkMissing_FallsBackToUsGracefullyWithoutCrash()
    {
        // Arrange: Typical Windows default setup with only en-US voices
        var mockProvider = new Mock<IVoiceProvider>();
        var voices = new List<VoiceDescriptor>
        {
            new("v-us-david", "Microsoft David", "en-US", "Male", IsDefault: true),
            new("v-us-zira", "Microsoft Zira", "en-US", "Female", IsDefault: false)
        };
        mockProvider.Setup(p => p.GetAllVoices()).Returns(voices);

        var resolver = new PronunciationVoiceResolver(mockProvider.Object);

        // Act
        var resUk = resolver.ResolveVoice("uk");

        // Assert
        resUk.IsExactMatch.Should().BeFalse();
        resUk.IsFallback.Should().BeTrue();
        resUk.Voice.Should().NotBeNull();
        resUk.Voice!.Language.Should().Be("en-US");
        resUk.FallbackReason.Should().Contain("Native British English (en-GB) voice is not installed on Windows");
        resolver.HasNativeVoice("uk").Should().BeFalse();
        resolver.HasNativeVoice("us").Should().BeTrue();
    }

    [Fact]
    public void ResolveVoice_WhenUkMissingButCommonwealthExists_FallsBackToCommonwealth()
    {
        // Arrange: Australian English installed, but no en-GB
        var mockProvider = new Mock<IVoiceProvider>();
        var voices = new List<VoiceDescriptor>
        {
            new("v-us", "Microsoft David", "en-US", "Male"),
            new("v-au", "Microsoft Catherine", "en-AU", "Female")
        };
        mockProvider.Setup(p => p.GetAllVoices()).Returns(voices);

        var resolver = new PronunciationVoiceResolver(mockProvider.Object);

        // Act
        var resUk = resolver.ResolveVoice("en-GB");

        // Assert
        resUk.IsExactMatch.Should().BeFalse();
        resUk.IsFallback.Should().BeTrue();
        resUk.Voice!.Id.Should().Be("v-au");
        resUk.FallbackReason.Should().Contain("Commonwealth English dialect");
    }

    [Fact]
    public void ResolveVoice_WhenNoVoicesAvailable_ReturnsSafeFallbackResult()
    {
        // Arrange
        var mockProvider = new Mock<IVoiceProvider>();
        mockProvider.Setup(p => p.GetAllVoices()).Returns(new List<VoiceDescriptor>());

        var resolver = new PronunciationVoiceResolver(mockProvider.Object);

        // Act
        var res = resolver.ResolveVoice("en-US");

        // Assert
        res.Voice.Should().BeNull();
        res.IsExactMatch.Should().BeFalse();
        res.IsFallback.Should().BeTrue();
        res.FallbackReason.Should().Contain("No speech synthesis voices available");
    }

    [Theory]
    [InlineData("uk", "en-GB")]
    [InlineData("en-gb", "en-GB")]
    [InlineData("GB", "en-GB")]
    [InlineData("british", "en-GB")]
    [InlineData("us", "en-US")]
    [InlineData("en-us", "en-US")]
    [InlineData("usa", "en-US")]
    [InlineData("american", "en-US")]
    [InlineData("", "en-GB")]
    [InlineData(null, "en-GB")]
    public void NormalizeLocale_ValidatesInputCorrectly(string? input, string expected)
    {
        var mockProvider = new Mock<IVoiceProvider>();
        var resolver = new PronunciationVoiceResolver(mockProvider.Object);

        var normalized = resolver.NormalizeLocale(input!);
        normalized.Should().Be(expected);
    }

    [Fact]
    public void RefreshVoices_ReloadsFromProvider()
    {
        // Arrange
        var mockProvider = new Mock<IVoiceProvider>();
        mockProvider.SetupSequence(p => p.GetAllVoices())
            .Returns(new List<VoiceDescriptor> { new("v1", "Voice 1", "en-US", "Male") })
            .Returns(new List<VoiceDescriptor> { new("v1", "Voice 1", "en-US", "Male"), new("v2", "Voice 2", "en-GB", "Female") });

        var resolver = new PronunciationVoiceResolver(mockProvider.Object);

        // Act & Assert 1: initially 1 voice
        resolver.GetAvailableVoices().Should().HaveCount(1);
        resolver.HasNativeVoice("en-GB").Should().BeFalse();

        // Act 2: Refresh
        resolver.RefreshVoices();
        resolver.GetAvailableVoices().Should().HaveCount(2);
        resolver.HasNativeVoice("en-GB").Should().BeTrue();
    }

    [Fact]
    public async Task AudioStorage_UsAndUkAudio_HaveDistinctCacheKeysAndDoNotCollide()
    {
        // Arrange
        var cacheDir = Path.Combine(_tempDir, "audio_cache");
        var storage = new LocalAudioStorageService(cacheDir);

        using var ukStream = new MemoryStream(Encoding.UTF8.GetBytes("UK audio wav data for schedule"));
        using var usStream = new MemoryStream(Encoding.UTF8.GetBytes("US audio wav data for schedule"));

        // Act
        var ukPath = await storage.CacheAudioAsync("schedule", "uk", ukStream);
        var usPath = await storage.CacheAudioAsync("schedule", "us", usStream);

        // Assert
        ukPath.Should().NotBeNull();
        usPath.Should().NotBeNull();
        ukPath.Should().NotBe(usPath);
        ukPath!.Should().Contain("schedule_uk");
        usPath!.Should().Contain("schedule_us");

        // Verify retrieval finds distinct paths
        var foundUk = storage.GetCachedAudioPath("schedule", "en-GB");
        var foundUs = storage.GetCachedAudioPath("schedule", "en-US");

        foundUk.Should().Be(ukPath);
        foundUs.Should().Be(usPath);
    }

    [Fact]
    public async Task UserPreferences_PreferredAccent_PersistsCorrectly()
    {
        // Arrange
        var prefDir = Path.Combine(_tempDir, "prefs");
        var prefService = new UserPreferencesService(NullLogger<UserPreferencesService>.Instance, prefDir);

        // Assert default
        prefService.GetPreferences().PreferredAccent.Should().Be("uk");

        // Act: Save US
        var capturedEvents = new List<UserPreferences>();
        prefService.PreferencesChanged += (_, p) => capturedEvents.Add(p);

        await prefService.SavePreferencesAsync(prefService.GetPreferences() with { PreferredAccent = "us" });

        // Assert updated
        prefService.GetPreferences().PreferredAccent.Should().Be("us");
        capturedEvents.Should().HaveCount(1);
        capturedEvents[0].PreferredAccent.Should().Be("us");

        // Reload from disk to verify persistence
        var reloadedService = new UserPreferencesService(NullLogger<UserPreferencesService>.Instance, prefDir);
        reloadedService.GetPreferences().PreferredAccent.Should().Be("us");
    }

    [Fact]
    public void ConcurrentVoiceResolution_ThreadSafe()
    {
        var mockProvider = new Mock<IVoiceProvider>();
        var voices = new List<VoiceDescriptor>
        {
            new("v-us", "Microsoft David", "en-US", "Male"),
            new("v-uk", "Microsoft George", "en-GB", "Male")
        };
        mockProvider.Setup(p => p.GetAllVoices()).Returns(voices);

        var resolver = new PronunciationVoiceResolver(mockProvider.Object);

        Parallel.For(0, 50, i =>
        {
            var accent = i % 2 == 0 ? "uk" : "us";
            var res = resolver.ResolveVoice(accent);
            res.Voice.Should().NotBeNull();
            res.IsExactMatch.Should().BeTrue();
        });
    }
}
