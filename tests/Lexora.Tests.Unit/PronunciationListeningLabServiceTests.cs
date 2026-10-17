using FluentAssertions;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class PronunciationListeningLabServiceTests
{
    private readonly Mock<IVocabularyRepository> _vocabRepoMock;
    private readonly PronunciationListeningLabService _labService;

    public PronunciationListeningLabServiceTests()
    {
        _vocabRepoMock = new Mock<IVocabularyRepository>();
        _labService = new PronunciationListeningLabService(_vocabRepoMock.Object);
    }

    [Fact]
    public void EvaluateDictation_ExactMatch_Returns100PercentAccuracy()
    {
        // Act
        var result = _labService.EvaluateDictation("schedule", "schedule", "/ˈʃedʒ.uːl/");

        // Assert
        result.IsExactMatch.Should().BeTrue();
        result.LevenshteinDistance.Should().Be(0);
        result.AccuracyPercent.Should().Be(100.0);
        result.FeedbackMessage.Should().Contain("Chính xác tuyệt đối");
    }

    [Fact]
    public void EvaluateDictation_CaseInsensitiveAndTrimming_ReturnsExactMatch()
    {
        // Act
        var result = _labService.EvaluateDictation("  SCHEDULE  ", "schedule", "/ˈʃedʒ.uːl/");

        // Assert
        result.IsExactMatch.Should().BeTrue();
        result.LevenshteinDistance.Should().Be(0);
        result.AccuracyPercent.Should().Be(100.0);
    }

    [Fact]
    public void EvaluateDictation_MinorTypo_CalculatesAccurateLevenshtein()
    {
        // Act: 'scheduel' vs 'schedule' (transposition / 2 edits)
        var result = _labService.EvaluateDictation("scheduel", "schedule", "/ˈʃedʒ.uːl/");

        // Assert
        result.IsExactMatch.Should().BeFalse();
        result.LevenshteinDistance.Should().Be(2);
        result.AccuracyPercent.Should().Be(75.0);
        result.FeedbackMessage.Should().Contain("Gần đúng");
    }

    [Fact]
    public void EvaluateDictation_CompletelyWrong_ReturnsLowAccuracy()
    {
        // Act
        var result = _labService.EvaluateDictation("elephant", "schedule", "/ˈʃedʒ.uːl/");

        // Assert
        result.IsExactMatch.Should().BeFalse();
        result.LevenshteinDistance.Should().BeGreaterThan(4);
        result.AccuracyPercent.Should().BeLessThan(50.0);
        result.FeedbackMessage.Should().Contain("Chưa chính xác");
    }

    [Fact]
    public async Task GetMinimalPairChallengesAsync_ReturnsExpectedCountAndValidPairs()
    {
        // Act
        var challenges = await _labService.GetMinimalPairChallengesAsync(5);

        // Assert
        challenges.Should().HaveCount(5);
        foreach (var challenge in challenges)
        {
            challenge.WordA.Should().NotBeNullOrWhiteSpace();
            challenge.WordB.Should().NotBeNullOrWhiteSpace();
            challenge.PhoneticA.Should().NotBeNullOrWhiteSpace();
            challenge.PhoneticB.Should().NotBeNullOrWhiteSpace();
            challenge.Tip.Should().NotBeNullOrWhiteSpace();
            challenge.PhonemePairLabel.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetMinimalPairChallengesAsync_TargetWordMatchesOneOfOptions()
    {
        // Act
        var challenges = await _labService.GetMinimalPairChallengesAsync(6);

        // Assert
        foreach (var challenge in challenges)
        {
            (challenge.TargetWord == challenge.WordA || challenge.TargetWord == challenge.WordB).Should().BeTrue();
            (challenge.TargetPhonetic == challenge.PhoneticA || challenge.TargetPhonetic == challenge.PhoneticB).Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetAccentComparisonListAsync_ReturnsUkAndUsAccentDifferences()
    {
        // Act
        var items = await _labService.GetAccentComparisonListAsync(5);

        // Assert
        items.Should().HaveCount(5);
        foreach (var item in items)
        {
            item.Term.Should().NotBeNullOrWhiteSpace();
            item.PhoneticUk.Should().NotBeNullOrWhiteSpace();
            item.PhoneticUs.Should().NotBeNullOrWhiteSpace();
            item.KeyDifferenceNote.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [InlineData("cat", "cat", 0)]
    [InlineData("cat", "hat", 1)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("", "hello", 5)]
    [InlineData("world", "", 5)]
    public void ComputeLevenshteinDistance_ReturnsCorrectDistance(string a, string b, int expected)
    {
        // Act
        int distance = PronunciationListeningLabService.ComputeLevenshteinDistance(a, b);

        // Assert
        distance.Should().Be(expected);
    }

    [Fact]
    public async Task GetNextDictationChallengeAsync_ReturnsChallengeFromRepository()
    {
        // Arrange
        var testWord = new Word
        {
            Term = "meticulous",
            NormalizedTerm = "meticulous",
            PhoneticUk = "/məˈtɪk.jə.ləs/",
            PhoneticUs = "/məˈtɪk.jə.ləs/",
            Level = CefrLevel.C1,
            PrimaryPartOfSpeech = PartOfSpeech.Adjective,
            Meanings = new List<WordMeaning>
            {
                new()
                {
                    PartOfSpeech = PartOfSpeech.Adjective,
                    DefinitionVi = "tỉ mỉ, cẩn thận",
                    DefinitionEn = "very careful and with great attention to every detail"
                }
            }
        };

        _vocabRepoMock.Setup(r => r.SearchAsync(
            It.IsAny<string?>(),
            It.IsAny<CefrLevel?>(),
            It.IsAny<PartOfSpeech?>(),
            It.IsAny<Guid?>(),
            It.IsAny<ExamType?>(),
            It.IsAny<LearningStatus?>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Word> { testWord });

        // Act
        var challenge = await _labService.GetNextDictationChallengeAsync();

        // Assert
        challenge.Should().NotBeNull();
        challenge!.Term.Should().Be("meticulous");
        challenge.PhoneticUk.Should().Be("/məˈtɪk.jə.ləs/");
        challenge.DefinitionVi.Should().Be("tỉ mỉ, cẩn thận");
        challenge.Level.Should().Be("C1");
    }
}
