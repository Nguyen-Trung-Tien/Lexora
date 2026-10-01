using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Enums;
using Xunit;

namespace Lexora.Tests.Unit;

public class GrammarSuggestionServiceTests
{
    private readonly GrammarSuggestionService _sut = new();

    private static GrammarRuleDto CreateGrammar(
        Guid id,
        string title,
        string structure,
        string explanationVi,
        string topic = "Tenses",
        string commonMistakes = "",
        List<GrammarExampleDto>? examples = null)
    {
        return new GrammarRuleDto(
            Id: id,
            Title: title,
            Structure: structure,
            ExplanationVi: explanationVi,
            ExplanationEn: "",
            CommonMistakes: commonMistakes,
            Level: CefrLevel.B1,
            ExamTarget: ExamType.General,
            Topic: topic,
            Examples: examples ?? new List<GrammarExampleDto>()
        );
    }

    private static WordDto CreateWord(Guid id, string term, string defVi)
    {
        return new WordDto(
            Id: id,
            Term: term,
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B1,
            PrimaryPartOfSpeech: PartOfSpeech.Verb,
            Notes: "",
            Meanings: new List<WordMeaningDto>
            {
                new(Guid.NewGuid(), PartOfSpeech.Verb, defVi, "", "", 0, new List<ExampleSentenceDto>())
            },
            Topics: new List<string>(),
            ExamTags: new List<string>(),
            LearningStatus: LearningStatus.New,
            NextReviewAt: null,
            Confidence: 0.0,
            IntervalDays: 0,
            EaseFactor: 2.5,
            Lapses: 0,
            Repetitions: 0,
            TotalReviews: 0,
            RetentionRate: 0.0
        );
    }

    [Fact]
    public void BuildSuggestions_DetectsConfusedGrammar_FromStandardPairs()
    {
        var presPerfId = Guid.NewGuid();
        var presPerf = CreateGrammar(presPerfId, "Present Perfect", "S + have/has + V3", "Hiện tại hoàn thành");
        var pastSimple = CreateGrammar(Guid.NewGuid(), "Past Simple", "S + V2/ed", "Quá khứ đơn");
        var passiveVoice = CreateGrammar(Guid.NewGuid(), "Passive Voice", "S + be + V3", "Câu bị động", topic: "Voice");

        var result = _sut.GetSuggestions(presPerf, new[] { presPerf, pastSimple, passiveVoice });

        result.ConfusedGrammar.Should().NotBeEmpty();
        result.ConfusedGrammar.Should().Contain(s => s.Rule.Title == "Past Simple");
    }

    [Fact]
    public void BuildSuggestions_DetectsSimilarStructures_WithSharedPatterns()
    {
        var cond1Id = Guid.NewGuid();
        var cond1 = CreateGrammar(cond1Id, "First Conditional", "If + S + V(s/es), S + will + V-inf", "Câu điều kiện loại 1", topic: "Conditionals");
        var cond2 = CreateGrammar(Guid.NewGuid(), "Second Conditional", "If + S + V2/ed, S + would + V-inf", "Câu điều kiện loại 2", topic: "Conditionals");
        var unrelated = CreateGrammar(Guid.NewGuid(), "Articles A/An/The", "a/an/the + Noun", "Mạo từ", topic: "Articles");

        var result = _sut.GetSuggestions(cond1, new[] { cond1, cond2, unrelated });

        result.SimilarStructures.Should().NotBeEmpty();
        result.SimilarStructures.Should().Contain(s => s.Rule.Title == "Second Conditional");
        result.SimilarStructures.Should().NotContain(s => s.Rule.Title == "Articles A/An/The");
    }

    [Fact]
    public void BuildSuggestions_ExtractsLinkedVocabulary_FromExamplesAndStructures()
    {
        var ruleId = Guid.NewGuid();
        var rule = CreateGrammar(
            ruleId,
            "Modal Verbs of Obligation",
            "S + must / have to + V",
            "Diễn tả nghĩa vụ bắt buộc",
            examples: new List<GrammarExampleDto>
            {
                new(Guid.NewGuid(), "You must submit the assignment before deadline.", "Bạn phải nộp bài tập trước hạn.", "")
            });

        var wordSubmit = CreateWord(Guid.NewGuid(), "submit", "nộp, đệ trình");
        var wordDeadline = CreateWord(Guid.NewGuid(), "deadline", "hạn chót");
        var wordKangaroo = CreateWord(Guid.NewGuid(), "kangaroo", "chuột túi");

        var result = _sut.GetSuggestions(
            rule,
            new[] { rule },
            new[] { wordSubmit, wordDeadline, wordKangaroo });

        result.RelatedVocabulary.Should().NotBeEmpty();
        result.RelatedVocabulary.Should().Contain(v => v.Word.Term == "submit" || v.Word.Term == "deadline");
        result.RelatedVocabulary.Should().NotContain(v => v.Word.Term == "kangaroo");
    }

    [Fact]
    public void BuildSuggestions_SameTopicRules_GroupsByTopic()
    {
        var ruleId = Guid.NewGuid();
        var passivePres = CreateGrammar(ruleId, "Present Simple Passive", "S + am/is/are + V3", "Bị động hiện tại đơn", topic: "Passive Voice");
        var passivePast = CreateGrammar(Guid.NewGuid(), "Past Simple Passive", "S + was/were + V3", "Bị động quá khứ đơn", topic: "Passive Voice");
        var relativeClause = CreateGrammar(Guid.NewGuid(), "Relative Clauses", "who / which / that", "Mệnh đề quan hệ", topic: "Clauses");

        var result = _sut.GetSuggestions(passivePres, new[] { passivePres, passivePast, relativeClause });

        result.SameTopicRules.Should().NotBeEmpty();
        result.SameTopicRules.Should().Contain(s => s.Rule.Title == "Past Simple Passive");
        result.SameTopicRules.Should().NotContain(s => s.Rule.Title == "Relative Clauses");
    }
}
