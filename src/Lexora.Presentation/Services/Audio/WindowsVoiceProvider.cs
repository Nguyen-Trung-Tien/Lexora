using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Windows.Media.SpeechSynthesis;

namespace Lexora.Presentation.Services;

public class WindowsVoiceProvider : IVoiceProvider
{
    public IReadOnlyList<VoiceDescriptor> GetAllVoices()
    {
        try
        {
            var defaultVoiceId = SpeechSynthesizer.DefaultVoice?.Id;
            var list = new List<VoiceDescriptor>();

            foreach (var v in SpeechSynthesizer.AllVoices)
            {
                list.Add(new VoiceDescriptor(
                    Id: v.Id,
                    DisplayName: v.DisplayName,
                    Language: v.Language,
                    Gender: v.Gender.ToString(),
                    IsDefault: string.Equals(v.Id, defaultVoiceId, StringComparison.OrdinalIgnoreCase)
                ));
            }

            return list;
        }
        catch
        {
            return Array.Empty<VoiceDescriptor>();
        }
    }
}
