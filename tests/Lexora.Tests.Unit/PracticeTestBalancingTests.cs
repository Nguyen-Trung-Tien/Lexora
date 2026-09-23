using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class PracticeTestBalancingTests
{
    private readonly Mock<IVocabularyRepository> _mockVocabRepo = new();
    private readonly Mock<IReviewRepository> _mockReviewRepo = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly TestService _testService;

    public PracticeTestBalancingTests()
    {
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockReviewRepo.Setup(r => r.CreateStudySessionAsync(It.IsAny<StudySession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudySession s, CancellationToken _) => s);

        _testService = new TestService(_mockVocabRepo.Object, _mockReviewRepo.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GenerateTestAsync_PosAwareDistractors_ShouldPreferSamePartOfSpeech()
    {
        // Arrange
        var targetVerb = new Word
        {
            Id = Guid.NewGuid(),
            Term = "accelerate",
            Level = CefrLevel.B2,
            PrimaryPartOfSpeech = PartOfSpeech.Verb,
            Meanings = new List<WordMeaning>
            {
                new() { PartOfSpeech = PartOfSpeech.Verb, DefinitionVi = "tăng tốc độ, gia tăng" }
            }
        };

        var otherVerbs = new List<Word>
        {
            new() { Id = Guid.NewGuid(), Term = "decelerate", Level = CefrLevel.B2, PrimaryPartOfSpeech = PartOfSpeech.Verb, Meanings = new List<WordMeaning> { new() { PartOfSpeech = PartOfSpeech.Verb, DefinitionVi = "giảm tốc độ" } } },
            new() { Id = Guid.NewGuid(), Term = "navigate", Level = CefrLevel.B2, PrimaryPartOfSpeech = PartOfSpeech.Verb, Meanings = new List<WordMeaning> { new() { PartOfSpeech = PartOfSpeech.Verb, DefinitionVi = "định hướng, điều hướng" } } },
            new() { Id = Guid.NewGuid(), Term = "propel", Level = CefrLevel.B2, PrimaryPartOfSpeech = PartOfSpeech.Verb, Meanings = new List<WordMeaning> { new() { PartOfSpeech = PartOfSpeech.Verb, DefinitionVi = "đẩy về phía trước" } } }
        };

        var nouns = new List<Word>
        {
            new() { Id = Guid.NewGuid(), Term = "acceleration", Level = CefrLevel.B2, PrimaryPartOfSpeech = PartOfSpeech.Noun, Meanings = new List<WordMeaning> { new() { PartOfSpeech = PartOfSpeech.Noun, DefinitionVi = "sự gia tốc" } } },
            new() { Id = Guid.NewGuid(), Term = "velocity", Level = CefrLevel.B2, PrimaryPartOfSpeech = PartOfSpeech.Noun, Meanings = new List<WordMeaning> { new() { PartOfSpeech = PartOfSpeech.Noun, DefinitionVi = "vận tốc" } } }
        };

        var allWords = new List<Word> { targetVerb };
        allWords.AddRange(otherVerbs);
        allWords.AddRange(nouns);

        _mockVocabRepo.Setup(r => r.SearchAsync(
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
            .ReturnsAsync(allWords);

        _mockVocabRepo.Setup(r => r.GetByIdsWithDetailsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => allWords.Where(w => ids.Contains(w.Id)).ToList());

        var config = new TestConfigurationDto(
            Type: TestType.WordToMeaning,
            QuestionCount: 1,
            Level: CefrLevel.B2,
            CustomWordIds: new List<Guid> { targetVerb.Id }
        );

        // Act
        var result = await _testService.GenerateTestAsync(config);

        // Assert
        result.Questions.Should().HaveCount(1);
        var q = result.Questions[0];
        q.Options.Should().HaveCount(4);
        q.Options.Should().Contain("tăng tốc độ, gia tăng");

        // The options should prioritize the definitions from other verbs (matching POS)
        var verbDefinitions = otherVerbs.Select(v => v.Meanings.First().DefinitionVi).ToList();
        var matchingVerbDistractorCount = q.Options.Count(opt => verbDefinitions.Contains(opt));
        matchingVerbDistractorCount.Should().Be(3);
    }
}
