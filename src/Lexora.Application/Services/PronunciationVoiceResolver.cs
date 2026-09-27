using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;

namespace Lexora.Application.Services;

public class PronunciationVoiceResolver : IPronunciationVoiceResolver
{
    private readonly IVoiceProvider _voiceProvider;
    private readonly object _lock = new();
    private IReadOnlyList<VoiceDescriptor>? _cachedVoices;

    public PronunciationVoiceResolver(IVoiceProvider voiceProvider)
    {
        _voiceProvider = voiceProvider;
    }

    public IReadOnlyList<VoiceDescriptor> GetAvailableVoices()
    {
        lock (_lock)
        {
            if (_cachedVoices == null)
            {
                _cachedVoices = _voiceProvider.GetAllVoices() ?? Array.Empty<VoiceDescriptor>();
            }
            return _cachedVoices;
        }
    }

    public void RefreshVoices()
    {
        lock (_lock)
        {
            _cachedVoices = _voiceProvider.GetAllVoices() ?? Array.Empty<VoiceDescriptor>();
        }
    }

    public string NormalizeLocale(string accentOrLocale)
    {
        if (string.IsNullOrWhiteSpace(accentOrLocale))
        {
            return "en-GB";
        }

        var trimmed = accentOrLocale.Trim().ToLowerInvariant();
        if (trimmed == "uk" || trimmed == "en-gb" || trimmed == "gb" || trimmed == "british")
        {
            return "en-GB";
        }

        if (trimmed == "us" || trimmed == "en-us" || trimmed == "usa" || trimmed == "american")
        {
            return "en-US";
        }

        if (trimmed.StartsWith("en-"))
        {
            var parts = trimmed.Split('-');
            return $"{parts[0].ToLowerInvariant()}-{parts[1].ToUpperInvariant()}";
        }

        return "en-GB";
    }

    public bool HasNativeVoice(string accentOrLocale)
    {
        var targetLocale = NormalizeLocale(accentOrLocale);
        var voices = GetAvailableVoices();
        return voices.Any(v => string.Equals(v.Language, targetLocale, StringComparison.OrdinalIgnoreCase));
    }

    public VoiceResolutionResult ResolveVoice(string accentOrLocale)
    {
        var targetLocale = NormalizeLocale(accentOrLocale);
        var voices = GetAvailableVoices();

        if (voices.Count == 0)
        {
            return new VoiceResolutionResult(
                targetLocale,
                null,
                IsExactMatch: false,
                IsFallback: true,
                FallbackReason: "No speech synthesis voices available on this system."
            );
        }

        if (targetLocale.Equals("en-GB", StringComparison.OrdinalIgnoreCase))
        {
            // 1. Exact en-GB match
            var exactUk = voices.FirstOrDefault(v => v.Language.Equals("en-GB", StringComparison.OrdinalIgnoreCase));
            if (exactUk != null)
            {
                return new VoiceResolutionResult("en-GB", exactUk, IsExactMatch: true, IsFallback: false, FallbackReason: null);
            }

            // 2. Commonwealth English dialect fallback
            var commonwealthDialects = new[] { "en-AU", "en-NZ", "en-IE", "en-CA", "en-ZA" };
            foreach (var dialect in commonwealthDialects)
            {
                var match = voices.FirstOrDefault(v => v.Language.Equals(dialect, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return new VoiceResolutionResult(
                        "en-GB",
                        match,
                        IsExactMatch: false,
                        IsFallback: true,
                        FallbackReason: $"Native British English (en-GB) voice not found; using Commonwealth English dialect ({match.Language} - {match.DisplayName})."
                    );
                }
            }

            // 3. Fallback to any English voice (usually en-US)
            var anyEnglish = voices.FirstOrDefault(v => v.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
            if (anyEnglish != null)
            {
                return new VoiceResolutionResult(
                    "en-GB",
                    anyEnglish,
                    IsExactMatch: false,
                    IsFallback: true,
                    FallbackReason: $"Native British English (en-GB) voice is not installed on Windows. Falling back to {anyEnglish.DisplayName} ({anyEnglish.Language})."
                );
            }

            // 4. Default system fallback
            var defaultVoice = voices.FirstOrDefault(v => v.IsDefault) ?? voices[0];
            return new VoiceResolutionResult(
                "en-GB",
                defaultVoice,
                IsExactMatch: false,
                IsFallback: true,
                FallbackReason: $"No English voice available. Falling back to default system voice ({defaultVoice.DisplayName})."
            );
        }
        else // en-US or others
        {
            // 1. Exact en-US match
            var exactUs = voices.FirstOrDefault(v => v.Language.Equals("en-US", StringComparison.OrdinalIgnoreCase));
            if (exactUs != null)
            {
                return new VoiceResolutionResult("en-US", exactUs, IsExactMatch: true, IsFallback: false, FallbackReason: null);
            }

            // 2. North American dialect (en-CA)
            var caMatch = voices.FirstOrDefault(v => v.Language.Equals("en-CA", StringComparison.OrdinalIgnoreCase));
            if (caMatch != null)
            {
                return new VoiceResolutionResult(
                    "en-US",
                    caMatch,
                    IsExactMatch: false,
                    IsFallback: true,
                    FallbackReason: $"Native en-US voice not found; using Canadian English ({caMatch.DisplayName})."
                );
            }

            // 3. Any English voice
            var anyEnglish = voices.FirstOrDefault(v => v.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
            if (anyEnglish != null)
            {
                return new VoiceResolutionResult(
                    "en-US",
                    anyEnglish,
                    IsExactMatch: false,
                    IsFallback: true,
                    FallbackReason: $"Native American English (en-US) voice is not installed on Windows. Falling back to {anyEnglish.DisplayName} ({anyEnglish.Language})."
                );
            }

            // 4. Default system fallback
            var defaultVoice = voices.FirstOrDefault(v => v.IsDefault) ?? voices[0];
            return new VoiceResolutionResult(
                "en-US",
                defaultVoice,
                IsExactMatch: false,
                IsFallback: true,
                FallbackReason: $"No English voice available. Falling back to default system voice ({defaultVoice.DisplayName})."
            );
        }
    }
}
