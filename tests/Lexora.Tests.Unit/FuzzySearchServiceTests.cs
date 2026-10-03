using FluentAssertions;
using Lexora.Application.Services;
using Xunit;

namespace Lexora.Tests.Unit;

public class FuzzySearchServiceTests
{
    private readonly FuzzySearchService _fuzzyService = new();

    [Theory]
    [InlineData("test", "test", 0)]
    [InlineData("cat", "hat", 1)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("book", "back", 2)]
    [InlineData("", "word", 4)]
    [InlineData("word", "", 4)]
    public void CalculateDistance_ShouldComputeCorrectLevenshteinDistance(string source, string target, int expectedDistance)
    {
        int distance = _fuzzyService.CalculateDistance(source, target);
        distance.Should().Be(expectedDistance);
    }

    [Fact]
    public void CalculateSimilarity_ShouldReturnOneForIdenticalStrings()
    {
        double similarity = _fuzzyService.CalculateSimilarity("resilience", "resilience");
        similarity.Should().Be(1.0);
    }

    [Fact]
    public void CalculateSimilarity_ShouldReturnHighValueForCloseStrings()
    {
        double similarity = _fuzzyService.CalculateSimilarity("accomodate", "accommodate");
        similarity.Should().BeGreaterThan(0.85);
    }

    [Fact]
    public void Search_ShouldPrioritizeExactAndPrefixMatches()
    {
        var words = new List<string> { "abandon", "ability", "able", "about", "above", "absent" };

        var results = _fuzzyService.Search("ab", words, w => w, maxResults: 10);

        results.Should().NotBeEmpty();
        results.All(r => r.IsExactOrPrefixMatch).Should().BeTrue();
    }

    [Fact]
    public void Search_ShouldFindFuzzyMatchesWhenQueryHasTypo()
    {
        var words = new List<string> { "vocabulary", "grammar", "pronunciation", "comprehension" };

        var results = _fuzzyService.Search("vocabuary", words, w => w, maxDistance: 2);

        results.Should().NotBeEmpty();
        results[0].Item.Should().Be("vocabulary");
    }

    [Fact]
    public void SuggestCorrection_ShouldSuggestCorrectionForCommonTypo()
    {
        var terms = new List<string> { "accommodate", "embarrass", "maintenance", "perseverance", "schedule" };

        string? suggestion = _fuzzyService.SuggestCorrection("embaras", terms);

        suggestion.Should().Be("embarrass");
    }

    [Fact]
    public void SuggestCorrection_ShouldReturnNullWhenWordAlreadyCorrect()
    {
        var terms = new List<string> { "schedule", "calendar", "deadline" };

        string? suggestion = _fuzzyService.SuggestCorrection("schedule", terms);

        suggestion.Should().BeNull();
    }

    [Theory]
    [InlineData("teh", "the", 1)]
    [InlineData("recieve", "receive", 1)]
    [InlineData("calss", "class", 1)]
    [InlineData("form", "from", 1)]
    public void CalculateDistance_AdjacentTransposition_YieldsDistanceOne(string source, string target, int expected)
    {
        int dist = _fuzzyService.CalculateDistance(source, target);
        dist.Should().Be(expected);
    }
}
