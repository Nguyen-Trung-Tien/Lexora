using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IPronunciationVoiceResolver
{
    IReadOnlyList<VoiceDescriptor> GetAvailableVoices();
    VoiceResolutionResult ResolveVoice(string accentOrLocale);
    bool HasNativeVoice(string accentOrLocale);
    string NormalizeLocale(string accentOrLocale);
    void RefreshVoices();
}
