using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IVocabularyKnowledgeGraphService
{
    Task<VocabularyKnowledgeGraphDto?> BuildGraphAsync(Guid wordId, CancellationToken cancellationToken = default);
    Task<VocabularyKnowledgeGraphDto?> BuildGraphForTermAsync(string term, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WordDto>> SearchGraphNodesAsync(string searchTerm, int limit = 15, CancellationToken cancellationToken = default);
    Task<KnowledgeGraphConceptsPagedResultDto> GetConceptsPagedAsync(KnowledgeGraphQueryDto query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeGraphRecommendationDto>> GetSmartRecommendationsAsync(int count = 8, CancellationToken cancellationToken = default);
}
