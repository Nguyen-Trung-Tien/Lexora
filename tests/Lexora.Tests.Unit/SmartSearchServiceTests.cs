using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Enums;
using Xunit;

namespace Lexora.Tests.Unit;

public class SmartSearchServiceTests
{
    private readonly SmartSearchService _sut = new();

    private static WordDto CreateWord(
        string term,
        string defVi,
        string defEn = "",
        string explanation = "",
        List<string>? topics = null,
        string notes = "")
    {
        return new WordDto(
            Id: Guid.NewGuid(),
            Term: term,
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B1,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: notes,
            Meanings: new List<WordMeaningDto>
            {
                new(Guid.NewGuid(), PartOfSpeech.Noun, defVi, defEn, explanation, 0, new List<ExampleSentenceDto>())
            },
            Topics: topics ?? new List<string>(),
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

    private static GrammarRuleDto CreateGrammar(
        string title,
        string structure,
        string explanationVi,
        string explanationEn = "",
        string topic = "Tenses",
        string commonMistakes = "")
    {
        return new GrammarRuleDto(
            Id: Guid.NewGuid(),
            Title: title,
            Structure: structure,
            ExplanationVi: explanationVi,
            ExplanationEn: explanationEn,
            CommonMistakes: commonMistakes,
            Level: CefrLevel.B1,
            ExamTarget: ExamType.General,
            Topic: topic,
            Examples: new List<GrammarExampleDto>()
        );
    }

    [Fact]
    public void ScoreWord_ExactMatch_YieldsHighestScore()
    {
        var word = CreateWord("ubiquitous", "phổ biến, có mặt khắp nơi");

        var result = _sut.ScoreWord("ubiquitous", word);

        result.Should().NotBeNull();
        result!.RelevanceScore.Should().Be(100);
        result.MatchType.Should().Be("Exact");
    }

    [Fact]
    public void ScoreWord_PrefixMatch_YieldsHighScore()
    {
        var word = CreateWord("benevolent", "nhân từ, rộng lượng");

        var result = _sut.ScoreWord("benev", word);

        result.Should().NotBeNull();
        result!.RelevanceScore.Should().BeInRange(85, 95);
        result.MatchType.Should().Be("Prefix");
    }

    [Fact]
    public void ScoreWord_VietnameseDiacritics_MatchesUnaccentedQuery()
    {
        var word = CreateWord("vocabulary", "từ vựng, vốn từ");

        // User types without Vietnamese accents
        var result = _sut.ScoreWord("tu vung", word);

        result.Should().NotBeNull();
        result!.RelevanceScore.Should().BeGreaterThan(50);
        result.MatchType.Should().BeOneOf("Keyword", "DefinitionVi", "Definition");
    }

    [Fact]
    public void ScoreWord_MultiTokenKeywords_MatchesAllTokens()
    {
        var word = CreateWord("compromise", "sự thỏa hiệp giữa hai bên", "agreement between two sides");

        var result = _sut.ScoreWord("thoa hiep hai ben", word);

        result.Should().NotBeNull();
        result!.RelevanceScore.Should().BeGreaterThan(50);
    }

    [Fact]
    public void ScoreWord_FuzzyMatch_HandlesMinorTypo()
    {
        var word = CreateWord("accomplish", "hoàn thành, đạt được");

        // Typo: acomplish (missing second 'c')
        var result = _sut.ScoreWord("acomplish", word);

        result.Should().NotBeNull();
        result!.RelevanceScore.Should().BeInRange(30, 60);
        result.MatchType.Should().Be("Fuzzy");
        result.MatchedField.Should().Be("Term");
    }

    [Fact]
    public void RankWords_OrdersByRelevanceCorrectly()
    {
        var exact = CreateWord("lead", "dẫn dắt, chỉ huy");
        var prefix = CreateWord("leader", "người lãnh đạo");
        var defMatch = CreateWord("guide", "người chỉ dẫn, lead others");
        var fuzzy = CreateWord("load", "tải trọng");

        var candidates = new[] { fuzzy, defMatch, prefix, exact };

        var ranked = _sut.RankWords("lead", candidates);

        ranked.Should().NotBeEmpty();
        ranked[0].Item.Term.Should().Be("lead", "Exact match must be ranked #1");
        ranked[1].Item.Term.Should().Be("leader", "Prefix match must be ranked #2");
    }

    [Fact]
    public void ScoreGrammar_StructureMatch_YieldsHighRelevance()
    {
        var rule = CreateGrammar(
            "Present Perfect",
            "S + have/has + V3/ed",
            "Thì hiện tại hoàn thành diễn tả hành động xảy ra trong quá khứ kéo dài đến hiện tại");

        var result = _sut.ScoreGrammar("have/has + V3", rule);

        result.Should().NotBeNull();
        result!.RelevanceScore.Should().BeGreaterThan(60);
        result.MatchType.Should().Be("Structure");
    }

    [Fact]
    public void FindClosestWordSuggestions_ReturnsRelevantTermsWhenNoExactMatch()
    {
        var pool = new[] { "apple", "application", "apply", "banana", "orange", "pineapple" };

        // Typo query
        var suggestions = _sut.FindClosestWordSuggestions("aple", pool, maxSuggestions: 3);

        suggestions.Should().NotBeEmpty();
        suggestions.Should().Contain("apple");
    }

    [Fact]
    public void FindClosestGrammarSuggestions_ReturnsClosestTitles()
    {
        var pool = new[] { "Present Simple", "Present Continuous", "Present Perfect", "Past Simple" };

        var suggestions = _sut.FindClosestGrammarSuggestions("Present Perfet", pool, maxSuggestions: 2);

        suggestions.Should().NotBeEmpty();
        suggestions[0].Should().Be("Present Perfect");
    }

    [Fact]
    public void RankWords_LargeDataset_ExecutesPerformantly()
    {
        var words = new List<WordDto>(5000);
        for (int i = 0; i < 5000; i++)
        {
            words.Add(CreateWord($"word_{i:D5}", $"nghĩa của từ thứ {i}", $"definition of word {i}"));
        }
        words.Add(CreateWord("serendipity", "sự tình cờ may mắn", "finding pleasant things by chance"));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var ranked = _sut.RankWords("serendipity", words, take: 10);
        sw.Stop();

        ranked.Should().NotBeEmpty();
        ranked[0].Item.Term.Should().Be("serendipity");
        sw.ElapsedMilliseconds.Should().BeLessThan(300, "5,000 words ranking should complete well within 300ms");
    }
}
