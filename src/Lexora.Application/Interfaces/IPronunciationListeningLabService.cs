using Lexora.Application.DTOs;
using Lexora.Domain.Enums;

namespace Lexora.Application.Interfaces;

public interface IPronunciationListeningLabService
{
    Task<ListeningDictationChallengeDto?> GetNextDictationChallengeAsync(
        CefrLevel? level = null,
        CancellationToken cancellationToken = default);

    DictationEvaluationResultDto EvaluateDictation(string userInput, string expectedTerm, string? phonetic = null);

    Task<IReadOnlyList<MinimalPairChallengeDto>> GetMinimalPairChallengesAsync(
        int count = 5,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccentComparisonItemDto>> GetAccentComparisonListAsync(
        int count = 10,
        CancellationToken cancellationToken = default);
}
