using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Enums;
using Xunit;

namespace Lexora.Tests.Unit;

public class SmartSearchCacheServiceTests
{
    private readonly SmartSearchCacheService _service = new();

    private static WordDto CreateWordDto(Guid id, string term)
    {
        return new WordDto(
            Id: id,
            Term: term,
            PhoneticUk: null,
            PhoneticUs: null,
            AudioUkPath: null,
            AudioUsPath: null,
            Level: CefrLevel.B1,
            PrimaryPartOfSpeech: PartOfSpeech.Noun,
            Notes: string.Empty,
            Meanings: new List<WordMeaningDto>(),
            Topics: new List<string>(),
            ExamTags: new List<string>(),
            LearningStatus: LearningStatus.Learning,
            NextReviewAt: null,
            Confidence: 0.0,
            IntervalDays: 0,
            EaseFactor: 2.5,
            Lapses: 0,
            Repetitions: 0,
            TotalReviews: 0,
            RetentionRate: 0.0);
    }

    [Fact]
    public async Task GetCachedWordsAsync_ShouldCacheResults_AndNotInvokeFactoryTwice()
    {
        // Arrange
        int factoryCallCount = 0;
        var words = new List<WordDto>
        {
            CreateWordDto(Guid.NewGuid(), "hello")
        };

        Task<IReadOnlyList<WordDto>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IReadOnlyList<WordDto>>(words);
        }

        // Act
        var result1 = await _service.GetCachedWordsAsync(Factory);
        var result2 = await _service.GetCachedWordsAsync(Factory);

        // Assert
        factoryCallCount.Should().Be(1);
        result1.Should().BeSameAs(words);
        result2.Should().BeSameAs(words);
    }

    [Fact]
    public async Task GetCachedWordsAsync_WhenForceRefresh_ShouldInvokeFactoryAgain()
    {
        // Arrange
        int factoryCallCount = 0;
        var words1 = new List<WordDto> { CreateWordDto(Guid.NewGuid(), "one") };
        var words2 = new List<WordDto> { CreateWordDto(Guid.NewGuid(), "two") };

        Task<IReadOnlyList<WordDto>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IReadOnlyList<WordDto>>(factoryCallCount == 1 ? words1 : words2);
        }

        // Act
        var result1 = await _service.GetCachedWordsAsync(Factory);
        var result2 = await _service.GetCachedWordsAsync(Factory, forceRefresh: true);

        // Assert
        factoryCallCount.Should().Be(2);
        result1.Should().BeSameAs(words1);
        result2.Should().BeSameAs(words2);
    }

    [Fact]
    public async Task InvalidateVocabulary_ShouldClearCache()
    {
        // Arrange
        int factoryCallCount = 0;
        var words1 = new List<WordDto> { CreateWordDto(Guid.NewGuid(), "one") };
        var words2 = new List<WordDto> { CreateWordDto(Guid.NewGuid(), "two") };

        Task<IReadOnlyList<WordDto>> Factory()
        {
            factoryCallCount++;
            return Task.FromResult<IReadOnlyList<WordDto>>(factoryCallCount == 1 ? words1 : words2);
        }

        // Act
        await _service.GetCachedWordsAsync(Factory);
        _service.InvalidateVocabulary();
        var result2 = await _service.GetCachedWordsAsync(Factory);

        // Assert
        factoryCallCount.Should().Be(2);
        result2.Should().BeSameAs(words2);
    }

    [Fact]
    public async Task InvalidationDuringFactoryExecution_ShouldNotCacheStaleResult()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<WordDto>>();
        var firstTask = _service.GetCachedWordsAsync(() => tcs.Task);

        // Invalidate while factory is in flight
        _service.InvalidateVocabulary();

        // Complete factory
        var staleWords = new List<WordDto> { CreateWordDto(Guid.NewGuid(), "stale") };
        tcs.SetResult(staleWords);

        var firstResult = await firstTask;
        firstResult.Should().BeSameAs(staleWords);

        // Subsequent call should invoke factory again because stale words were NOT cached
        int freshCallCount = 0;
        var freshWords = new List<WordDto> { CreateWordDto(Guid.NewGuid(), "fresh") };
        var secondResult = await _service.GetCachedWordsAsync(() =>
        {
            freshCallCount++;
            return Task.FromResult<IReadOnlyList<WordDto>>(freshWords);
        });

        freshCallCount.Should().Be(1);
        secondResult.Should().BeSameAs(freshWords);
    }
}
