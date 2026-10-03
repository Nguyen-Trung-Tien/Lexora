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
    public void FindClosestGrammarSuggestions_SingleWordTypo_MatchesMultiWordTitle()
    {
        var pool = new[]
        {
            "Conditional Sentences Type 1",
            "Present Simple Tense",
            "Passive Voice with Modals",
            "Inversion with Negative Adverbials"
        };

        var suggestions = _sut.FindClosestGrammarSuggestions("conditonal", pool, maxSuggestions: 2);

        suggestions.Should().NotBeEmpty();
        suggestions[0].Should().Be("Conditional Sentences Type 1");
    }

    [Fact]
    public void FindClosestGrammarSuggestions_MultiWordTypo_MatchesCorrectTitle()
    {
        var pool = new[]
        {
            "Conditional Sentences Type 1",
            "Present Simple Tense",
            "Past Continuous Tense"
        };

        var suggestions = _sut.FindClosestGrammarSuggestions("past continous", pool, maxSuggestions: 2);

        suggestions.Should().NotBeEmpty();
        suggestions[0].Should().Be("Past Continuous Tense");
    }

    [Fact]
    public void FindClosestGrammarRuleSuggestions_VietnameseQuery_MatchesRuleExplanation()
    {
        var rules = new[]
        {
            CreateGrammar("Conditional Sentences Type 1", "If + S + V(s/es)", "Câu điều kiện loại 1 diễn tả điều kiện có thể xảy ra ở hiện tại hoặc tương lai."),
            CreateGrammar("Present Simple", "S + V(s/es)", "Thì hiện tại đơn diễn tả chân lý, thói quen."),
            CreateGrammar("Passive Voice", "S + be + V3/ed", "Câu bị động diễn tả hành động được tác động.")
        };

        var suggestions = _sut.FindClosestGrammarRuleSuggestions("cau dieu kien", rules, maxSuggestions: 2);

        suggestions.Should().NotBeEmpty();
        suggestions[0].Should().Be("Conditional Sentences Type 1");
    }

    [Fact]
    public void FindClosestWordSuggestions_TranspositionTypo_MatchesWord()
    {
        var pool = new[] { "receive", "believe", "achieve", "the", "their" };

        var suggestionsRecieve = _sut.FindClosestWordSuggestions("recieve", pool, maxSuggestions: 2);
        suggestionsRecieve.Should().NotBeEmpty();
        suggestionsRecieve[0].Should().Be("receive");

        var suggestionsTeh = _sut.FindClosestWordSuggestions("teh", pool, maxSuggestions: 2);
        suggestionsTeh.Should().NotBeEmpty();
        suggestionsTeh[0].Should().Be("the");
    }

    [Fact]
    public void FindClosestWordSuggestions_WithWordDtos_MatchesVietnameseDefinition()
    {
        var words = new[]
        {
            CreateWord("abandon", "từ bỏ, ruồng bỏ"),
            CreateWord("accomplish", "hoàn thành, đạt được mục tiêu"),
            CreateWord("resilience", "khả năng phục hồi, kiên cường")
        };

        var suggestions = _sut.FindClosestWordSuggestions("tu bo", words, maxSuggestions: 2);

        suggestions.Should().NotBeEmpty();
        suggestions[0].Should().Be("abandon");
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
