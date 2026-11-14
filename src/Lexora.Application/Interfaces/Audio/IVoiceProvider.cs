using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IVoiceProvider
{
    IReadOnlyList<VoiceDescriptor> GetAllVoices();
}
