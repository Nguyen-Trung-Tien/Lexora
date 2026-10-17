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
        string RelationshipLabel)
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
}
