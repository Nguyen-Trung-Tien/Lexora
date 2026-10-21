using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class VocabularyKnowledgeGraphService : IVocabularyKnowledgeGraphService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IVocabularySuggestionService _suggestionService;

    public VocabularyKnowledgeGraphService(
        IVocabularyRepository vocabRepo,
        IVocabularySuggestionService suggestionService)
    {
        _vocabRepo = vocabRepo;
        _suggestionService = suggestionService;
    }

    public async Task<VocabularyKnowledgeGraphDto?> BuildGraphAsync(Guid wordId, CancellationToken cancellationToken = default)
    {
        var word = await _vocabRepo.GetWithDetailsAsync(wordId, cancellationToken);
        if (word == null) return null;

        return await BuildGraphInternalAsync(word, cancellationToken);
    }

    public async Task<VocabularyKnowledgeGraphDto?> BuildGraphForTermAsync(string term, CancellationToken cancellationToken = default)
    {
        var normalized = term.Trim().ToLowerInvariant();
        var matches = await _vocabRepo.SearchAsync(searchTerm: normalized, take: 5, cancellationToken: cancellationToken);
        var exact = matches.FirstOrDefault(w => string.Equals(w.NormalizedTerm, normalized, StringComparison.OrdinalIgnoreCase))
                    ?? matches.FirstOrDefault();

        if (exact == null) return null;

        var fullWord = await _vocabRepo.GetWithDetailsAsync(exact.Id, cancellationToken);
        return fullWord == null ? null : await BuildGraphInternalAsync(fullWord, cancellationToken);
    }

    public async Task<IReadOnlyList<WordDto>> SearchGraphNodesAsync(string searchTerm, int limit = 15, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            var popular = await _vocabRepo.SearchAsync(take: limit, cancellationToken: cancellationToken);
            return popular.Select(MapToWordDto).ToList();
        }

        var results = await _vocabRepo.SearchAsync(searchTerm: searchTerm, take: limit, cancellationToken: cancellationToken);
        return results.Select(MapToWordDto).ToList();
    }

    public async Task<KnowledgeGraphConceptsPagedResultDto> GetConceptsPagedAsync(KnowledgeGraphQueryDto query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var totalCount = await _vocabRepo.CountFilteredAsync(
            searchTerm: query.SearchTerm,
            level: query.Level,
            partOfSpeech: query.PartOfSpeech,
            cancellationToken: cancellationToken);

        var words = await _vocabRepo.SearchAsync(
            searchTerm: query.SearchTerm,
            level: query.Level,
            partOfSpeech: query.PartOfSpeech,
            skip: skip,
            take: pageSize,
            sortBy: query.SortBy ?? "term",
            isAscending: true,
            cancellationToken: cancellationToken);

        var wordList = words ?? Array.Empty<Word>();
        var items = new List<KnowledgeGraphConceptSummaryDto>();
        foreach (var w in wordList)
        {
            var firstMeaning = w.Meanings?.FirstOrDefault();
            var topicsCount = w.WordTopics?.Count ?? 0;
            var (synCount, antCount) = ExtractNotesRelationCounts(w.Notes);
            var familyCount = EstimateWordFamilyCount(w.Term);
            var collocationsCount = w.Meanings?.Sum(m => m.Examples?.Count ?? 0) ?? 0;
            var totalConn = Math.Max(1, synCount + antCount + familyCount + (topicsCount * 2) + Math.Min(collocationsCount, 4));

            items.Add(new KnowledgeGraphConceptSummaryDto(
                Id: w.Id,
                Term: w.Term,
                Level: w.Level,
                PartOfSpeech: w.PrimaryPartOfSpeech,
                Ipa: w.PhoneticUs ?? w.PhoneticUk,
                AudioPath: w.AudioUsPath ?? w.AudioUkPath,
                MeaningVi: firstMeaning?.DefinitionVi ?? string.Empty,
                TotalConnections: totalConn,
                SynonymsCount: synCount,
                AntonymsCount: antCount,
                WordFamilyCount: familyCount,
                CollocationsCount: Math.Min(collocationsCount, 5),
                TopicCount: topicsCount,
                IsMastered: w.ReviewCard?.Status == LearningStatus.Mastered
            ));
        }

        return new KnowledgeGraphConceptsPagedResultDto(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<KnowledgeGraphRecommendationDto>> GetSmartRecommendationsAsync(int count = 8, CancellationToken cancellationToken = default)
    {
        var candidates = await _vocabRepo.SearchAsync(
            take: Math.Max(20, count * 3),
            sortBy: "term",
            isAscending: true,
            cancellationToken: cancellationToken);

        var candidateList = candidates ?? Array.Empty<Word>();
        if (candidateList.Count == 0) return Array.Empty<KnowledgeGraphRecommendationDto>();

        var selected = candidateList
            .Where(w => w.Level >= CefrLevel.B1 && (w.Meanings?.Count ?? 0) > 0)
            .OrderByDescending(w => w.WordTopics?.Count ?? 0)
            .ThenByDescending(w => w.Meanings?.Count ?? 0)
            .Take(count)
            .ToList();

        if (selected.Count < count)
        {
            selected.AddRange(candidateList.Except(selected).Take(count - selected.Count));
        }

        var list = new List<KnowledgeGraphRecommendationDto>();
        foreach (var w in selected)
        {
            var firstMeaning = w.Meanings?.FirstOrDefault();
            var reason = w.Level switch
            {
                CefrLevel.C1 or CefrLevel.C2 => "Khái niệm học thuật nâng cao với mạng nghĩa đa tầng",
                CefrLevel.B2 => "Từ vựng trọng tâm kỳ thi (TOEIC 750+ / IELTS 6.5+)",
                CefrLevel.B1 => "Khái niệm cốt lõi có nhiều từ đồng nghĩa và họ từ phong phú",
                _ => "Từ vựng thông dụng với liên kết ngữ nghĩa phong phú"
            };

            int estimated = Math.Max(4, ((w.WordTopics?.Count ?? 0) * 2) + ((w.Meanings?.Count ?? 0) * 2));

            list.Add(new KnowledgeGraphRecommendationDto(
                w.Id,
                w.Term,
                firstMeaning?.DefinitionVi ?? string.Empty,
                w.Level,
                w.PrimaryPartOfSpeech,
                reason,
                estimated
            ));
        }

        return list;
    }

    private async Task<VocabularyKnowledgeGraphDto> BuildGraphInternalAsync(Word targetWord, CancellationToken cancellationToken)
    {
        var suggestions = await _suggestionService.GetSuggestionsAsync(targetWord.Id, cancellationToken);

        var firstMeaning = targetWord.Meanings.FirstOrDefault();
        var firstExample = firstMeaning?.Examples.FirstOrDefault();
        var centerNode = new GraphNodeDto(
            Id: targetWord.Id,
            Term: targetWord.Term,
            NodeType: KnowledgeGraphNodeType.Center,
            MeaningVi: firstMeaning?.DefinitionVi ?? string.Empty,
            Level: targetWord.Level,
            PartOfSpeech: targetWord.PrimaryPartOfSpeech,
            Ipa: targetWord.PhoneticUs ?? targetWord.PhoneticUk,
            AudioPath: targetWord.AudioUsPath ?? targetWord.AudioUkPath,
            IsMastered: targetWord.ReviewCard?.Status == LearningStatus.Mastered,
            IsCenter: true,
            ConnectionWeight: 1.0,
            RelationshipLabel: "Core Concept",
            MeaningEn: firstMeaning?.DefinitionEn ?? string.Empty,
            Explanation: firstMeaning?.Explanation ?? string.Empty,
            ExampleSentenceEn: firstExample?.SentenceEn ?? string.Empty,
            ExampleSentenceVi: firstExample?.SentenceVi ?? string.Empty
        );

        var connectedNodes = new List<GraphNodeDto>();
        var edges = new List<GraphEdgeDto>();

        // 1. Synonyms
        foreach (var item in suggestions.Synonyms.Take(6))
        {
            var node = CreateNodeFromSuggestion(item, KnowledgeGraphNodeType.Synonym, 0.9, "Synonym (Đồng nghĩa)");
            connectedNodes.Add(node);
            edges.Add(new GraphEdgeDto(centerNode.Id, node.Id, centerNode.Term, node.Term, KnowledgeGraphNodeType.Synonym, "Synonym"));
        }

        // 2. Antonyms
        foreach (var item in suggestions.Antonyms.Take(4))
        {
            var node = CreateNodeFromSuggestion(item, KnowledgeGraphNodeType.Antonym, 0.85, "Antonym (Trái nghĩa)");
            connectedNodes.Add(node);
            edges.Add(new GraphEdgeDto(centerNode.Id, node.Id, centerNode.Term, node.Term, KnowledgeGraphNodeType.Antonym, "Antonym"));
        }

        // 3. Word Family / Derived Forms
        foreach (var item in suggestions.WordFamily.Take(5))
        {
            var node = CreateNodeFromSuggestion(item, KnowledgeGraphNodeType.WordFamily, 0.8, "Word Family (Họ từ)");
            connectedNodes.Add(node);
            edges.Add(new GraphEdgeDto(centerNode.Id, node.Id, centerNode.Term, node.Term, KnowledgeGraphNodeType.WordFamily, "Derived Form"));
        }

        // 4. Collocations & Phrases
        foreach (var item in suggestions.Collocations.Take(5))
        {
            var node = CreateNodeFromSuggestion(item, KnowledgeGraphNodeType.Collocation, 0.75, "Collocation (Cụm kết hợp)");
            connectedNodes.Add(node);
            edges.Add(new GraphEdgeDto(centerNode.Id, node.Id, centerNode.Term, node.Term, KnowledgeGraphNodeType.Collocation, "Collocation"));
        }

        // 5. Topic Semantic Cluster
        foreach (var item in suggestions.SameTopic.Take(4))
        {
            var node = CreateNodeFromSuggestion(item, KnowledgeGraphNodeType.TopicCluster, 0.65, "Topic Domain (Cùng chủ đề)");
            connectedNodes.Add(node);
            edges.Add(new GraphEdgeDto(centerNode.Id, node.Id, centerNode.Term, node.Term, KnowledgeGraphNodeType.TopicCluster, "Same Domain"));
        }

        // Fallback connections if sparse
        if (connectedNodes.Count < 3)
        {
            var topicId = targetWord.WordTopics?.FirstOrDefault()?.TopicId;
            var relatedFallback = await _vocabRepo.SearchAsync(
                level: targetWord.Level,
                topicId: topicId,
                take: 6,
                cancellationToken: cancellationToken);

            var fallbackList = relatedFallback ?? Array.Empty<Word>();
            foreach (var fallback in fallbackList.Where(w => w.Id != targetWord.Id))
            {
                if (connectedNodes.Any(n => n.Id == fallback.Id)) continue;

                var fMeaning = fallback.Meanings.FirstOrDefault();
                var node = new GraphNodeDto(
                    Id: fallback.Id,
                    Term: fallback.Term,
                    NodeType: KnowledgeGraphNodeType.TopicCluster,
                    MeaningVi: fMeaning?.DefinitionVi ?? string.Empty,
                    Level: fallback.Level,
                    PartOfSpeech: fallback.PrimaryPartOfSpeech,
                    Ipa: fallback.PhoneticUs ?? fallback.PhoneticUk,
                    AudioPath: fallback.AudioUsPath ?? fallback.AudioUkPath,
                    IsMastered: fallback.ReviewCard?.Status == LearningStatus.Mastered,
                    IsCenter: false,
                    ConnectionWeight: 0.6,
                    RelationshipLabel: $"Same CEFR {fallback.Level}"
                );
                connectedNodes.Add(node);
                edges.Add(new GraphEdgeDto(centerNode.Id, node.Id, centerNode.Term, node.Term, KnowledgeGraphNodeType.TopicCluster, "Shared Level"));
            }
        }

        return new VocabularyKnowledgeGraphDto(
            CenterNode: centerNode,
            ConnectedNodes: connectedNodes,
            Edges: edges,
            SynonymsCount: suggestions.Synonyms.Count,
            AntonymsCount: suggestions.Antonyms.Count,
            WordFamilyCount: suggestions.WordFamily.Count,
            CollocationsCount: suggestions.Collocations.Count,
            TopicWordsCount: suggestions.SameTopic.Count,
            TotalConnections: connectedNodes.Count
        );
    }

    private static GraphNodeDto CreateNodeFromSuggestion(
        WordSuggestionItemDto item,
        KnowledgeGraphNodeType nodeType,
        double weight,
        string relationshipLabel)
    {
        var word = item.Word;
        var firstMeaning = word?.Meanings.FirstOrDefault();
        var firstExample = firstMeaning?.Examples.FirstOrDefault();

        return new GraphNodeDto(
            Id: word?.Id ?? Guid.NewGuid(),
            Term: item.Term,
            NodeType: nodeType,
            MeaningVi: !string.IsNullOrWhiteSpace(item.PrimaryDefinition) ? item.PrimaryDefinition : (firstMeaning?.DefinitionVi ?? string.Empty),
            Level: word?.Level ?? CefrLevel.None,
            PartOfSpeech: word?.PrimaryPartOfSpeech ?? PartOfSpeech.Noun,
            Ipa: word?.PhoneticUs ?? word?.PhoneticUk,
            AudioPath: word?.AudioUsPath ?? word?.AudioUkPath,
            IsMastered: word?.LearningStatus == LearningStatus.Mastered,
            IsCenter: false,
            ConnectionWeight: weight,
            RelationshipLabel: relationshipLabel,
            MeaningEn: firstMeaning?.DefinitionEn ?? string.Empty,
            Explanation: item.Reason,
            ExampleSentenceEn: firstExample?.SentenceEn ?? string.Empty,
            ExampleSentenceVi: firstExample?.SentenceVi ?? string.Empty
        );
    }

    private static (int synCount, int antCount) ExtractNotesRelationCounts(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return (0, 0);
        int syn = 0, ant = 0;
        var lower = notes.ToLowerInvariant();
        if (lower.Contains("syn:") || lower.Contains("đồng nghĩa:"))
            syn += 2;
        if (lower.Contains("ant:") || lower.Contains("opp:") || lower.Contains("trái nghĩa:"))
            ant += 2;
        return (syn, ant);
    }

    private static int EstimateWordFamilyCount(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 4) return 0;
        return term.Length >= 6 ? 3 : 1;
    }

    private static WordDto MapToWordDto(Word w)
    {
        var meanings = w.Meanings.Select(m => new WordMeaningDto(
            m.Id,
            m.PartOfSpeech,
            m.DefinitionVi,
            m.DefinitionEn,
            m.Explanation,
            m.OrderIndex,
            m.Examples.Select(e => new ExampleSentenceDto(e.Id, e.SentenceEn, e.SentenceVi, e.AudioPath)).ToList()
        )).ToList();

        return new WordDto(
            Id: w.Id,
            Term: w.Term,
            PhoneticUk: w.PhoneticUk,
            PhoneticUs: w.PhoneticUs,
            AudioUkPath: w.AudioUkPath,
            AudioUsPath: w.AudioUsPath,
            Level: w.Level,
            PrimaryPartOfSpeech: w.PrimaryPartOfSpeech,
            Notes: w.Notes,
            Meanings: meanings,
            Topics: w.WordTopics.Select(wt => wt.Topic?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList(),
            ExamTags: w.WordExamTags.Select(wet => wet.ExamTag?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList(),
            LearningStatus: w.ReviewCard?.Status,
            NextReviewAt: w.ReviewCard?.NextReviewAt,
            Confidence: w.ReviewCard?.Confidence ?? 0.0,
            IntervalDays: w.ReviewCard?.IntervalDays ?? 0,
            EaseFactor: w.ReviewCard?.EaseFactor ?? 2.5,
            Lapses: w.ReviewCard?.Lapses ?? 0,
            Repetitions: w.ReviewCard?.Repetitions ?? 0,
            TotalReviews: w.ReviewCard?.TotalReviews ?? 0,
            RetentionRate: 100.0
        );
    }
}
