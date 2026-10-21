using Lexora.Domain.Enums;

namespace Lexora.Application.DTOs;

public enum KnowledgeGraphNodeType
{
    Center = 0,
    Synonym = 1,
    Antonym = 2,
    WordFamily = 3,
    Collocation = 4,
    TopicCluster = 5
}

public class GraphNodeDto
{
    public Guid Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public KnowledgeGraphNodeType NodeType { get; set; }
    public string MeaningVi { get; set; } = string.Empty;
    public CefrLevel Level { get; set; }
    public PartOfSpeech PartOfSpeech { get; set; }
    public string? Ipa { get; set; }
    public string? AudioPath { get; set; }
    public bool IsMastered { get; set; }
    public bool IsCenter { get; set; }
    public double ConnectionWeight { get; set; }
    public string RelationshipLabel { get; set; } = string.Empty;

    public string MeaningEn { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string ExampleSentenceEn { get; set; } = string.Empty;
    public string ExampleSentenceVi { get; set; } = string.Empty;

    public GraphNodeDto() { }

    public GraphNodeDto(
        Guid Id,
        string Term,
        KnowledgeGraphNodeType NodeType,
        string MeaningVi,
        CefrLevel Level,
        PartOfSpeech PartOfSpeech,
        string? Ipa,
        string? AudioPath,
        bool IsMastered,
        bool IsCenter,
        double ConnectionWeight,
        string RelationshipLabel,
        string MeaningEn = "",
        string Explanation = "",
        string ExampleSentenceEn = "",
        string ExampleSentenceVi = "")
    {
        this.Id = Id;
        this.Term = Term;
        this.NodeType = NodeType;
        this.MeaningVi = MeaningVi;
        this.Level = Level;
        this.PartOfSpeech = PartOfSpeech;
        this.Ipa = Ipa;
        this.AudioPath = AudioPath;
        this.IsMastered = IsMastered;
        this.IsCenter = IsCenter;
        this.ConnectionWeight = ConnectionWeight;
        this.RelationshipLabel = RelationshipLabel;
        this.MeaningEn = MeaningEn;
        this.Explanation = Explanation;
        this.ExampleSentenceEn = ExampleSentenceEn;
        this.ExampleSentenceVi = ExampleSentenceVi;
    }
}

public record GraphEdgeDto(
    Guid SourceId,
    Guid TargetId,
    string SourceTerm,
    string TargetTerm,
    KnowledgeGraphNodeType RelationshipType,
    string RelationshipLabel
);

public record VocabularyKnowledgeGraphDto(
    GraphNodeDto CenterNode,
    IReadOnlyList<GraphNodeDto> ConnectedNodes,
    IReadOnlyList<GraphEdgeDto> Edges,
    int SynonymsCount,
    int AntonymsCount,
    int WordFamilyCount,
    int CollocationsCount,
    int TopicWordsCount,
    int TotalConnections
)
{
    public static VocabularyKnowledgeGraphDto Empty => new(
        new GraphNodeDto(Guid.Empty, string.Empty, KnowledgeGraphNodeType.Center, string.Empty, CefrLevel.A1, PartOfSpeech.Noun, null, null, false, true, 1.0, string.Empty),
        [],
        [],
        0, 0, 0, 0, 0, 0
    );

    public bool HasConnections => TotalConnections > 0;
}

public record KnowledgeGraphQueryDto(
    string? SearchTerm = null,
    CefrLevel? Level = null,
    PartOfSpeech? PartOfSpeech = null,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = "term"
);

public record KnowledgeGraphConceptSummaryDto(
    Guid Id,
    string Term,
    CefrLevel Level,
    PartOfSpeech PartOfSpeech,
    string? Ipa,
    string? AudioPath,
    string MeaningVi,
    int TotalConnections,
    int SynonymsCount,
    int AntonymsCount,
    int WordFamilyCount,
    int CollocationsCount,
    int TopicCount,
    bool IsMastered
);

public record KnowledgeGraphConceptsPagedResultDto(
    IReadOnlyList<KnowledgeGraphConceptSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public int FromItemIndex => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int ToItemIndex => TotalCount == 0 ? 0 : Math.Min(Page * PageSize, TotalCount);
}

public record KnowledgeGraphRecommendationDto(
    Guid Id,
    string Term,
    string MeaningVi,
    CefrLevel Level,
    PartOfSpeech PartOfSpeech,
    string Reason,
    int EstimatedConnections
);
