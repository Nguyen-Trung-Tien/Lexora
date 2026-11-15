using System.Security;
using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Application.Services.Phrases;
using Lexora.Domain.Entities;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class ConversationPhraseUpgradeTests
{
    private readonly Mock<IConversationPhraseRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IReviewRepository> _reviewRepoMock = new();
    private readonly Mock<IVocabularyRepository> _vocabRepoMock = new();

    private ConversationPhraseService CreateSut(bool withReviewSync = false) =>
        withReviewSync
            ? new ConversationPhraseService(
                _repoMock.Object,
                _unitOfWorkMock.Object,
                fuzzySearchService: new FuzzySearchService(),
                reviewRepository: _reviewRepoMock.Object,
                vocabularyRepository: _vocabRepoMock.Object)
            : new ConversationPhraseService(
                _repoMock.Object,
                _unitOfWorkMock.Object,
                fuzzySearchService: new FuzzySearchService());

    [Fact]
    public async Task GetPhrasesAsync_WithRegisterExamAndDueFilters_ShouldInvokeAdvancedRepositoryQuery()
    {
        // Arrange
        var phrase = new ConversationPhrase
        {
            Category = "Business & TOEIC",
            PromptSentence = "Could you share the budget forecast?",
            SampleResponse = "Sure, I will send it over shortly.",
            Register = PhraseRegister.Formal,
            ExamTarget = ExamTarget.Toeic,
            NextReviewAt = DateTime.UtcNow.AddHours(-1)
        };
        phrase.UpdateNormalizedFields();

        _repoMock.Setup(r => r.GetFilteredAdvancedAsync(
                "Business & TOEIC",
                CefrLevel.B2,
                PhraseRegister.Formal,
                ExamTarget.Toeic,
                true,
                true,
                "budget",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([phrase]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPhrasesAsync(new ConversationPhraseFilterDto
        {
            Category = "Business & TOEIC",
            CefrLevel = CefrLevel.B2,
            Register = PhraseRegister.Formal,
            ExamTarget = ExamTarget.Toeic,
            OnlyFavorites = true,
            OnlyDueForReview = true,
            SearchQuery = "budget"
        });

        // Assert
        result.Should().HaveCount(1);
        result[0].Register.Should().Be(PhraseRegister.Formal);
        result[0].ExamTarget.Should().Be(ExamTarget.Toeic);
        _repoMock.Verify(r => r.GetFilteredAdvancedAsync(
            "Business & TOEIC", CefrLevel.B2, PhraseRegister.Formal, ExamTarget.Toeic,
            true, true, "budget", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPhrasesAsync_WhenExactSearchEmpty_ShouldFallbackToFuzzyMatching()
    {
        // Arrange
        var candidate = new ConversationPhrase
        {
            Category = "Business & TOEIC",
            ContextTag = "Client Deliverable",
            PromptSentence = "Could you clarify the deliverable timeline?",
            SampleResponse = "Certainly, we target Friday."
        };
        candidate.UpdateNormalizedFields();

        _repoMock.Setup(r => r.GetFilteredAsync(
                null, null, null, "delivrable", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _repoMock.Setup(r => r.GetFilteredAsync(
                null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([candidate]);

        var sut = CreateSut();

        // Act - intentional typo "delivrable"
        var result = await sut.GetPhrasesAsync(new ConversationPhraseFilterDto
        {
            SearchQuery = "delivrable"
        });

        // Assert
        result.Should().ContainSingle(p => p.Id == candidate.Id);
    }

    [Fact]
    public async Task ImportSmartAsync_Exceeding20MbLimit_ShouldRejectAndNotTouchDatabase()
    {
        // Arrange
        var hugePayload = new string('a', 20 * 1024 * 1024 + 5);
        var sut = CreateSut();

        // Act
        var result = await sut.ImportSmartAsync(hugePayload, "large.json");

        // Assert
        result.TotalImported.Should().Be(0);
        result.Errors.Should().ContainSingle(e => e.Contains("20 MB"));
        _repoMock.Verify(r => r.AddAsync(It.IsAny<ConversationPhrase>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportSmartAsync_ShouldTrimNormalizeAndDeduplicateConflicts()
    {
        // Arrange
        var existing = new ConversationPhrase
        {
            Category = "Travel & Airport",
            PromptSentence = "Could you help me rebook my flight?",
            SampleResponse = "Old response"
        };
        existing.UpdateNormalizedFields();

        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        var json = """
        [
          {
            "category": "  Travel & Airport  ",
            "promptSentence": "  could you help me rebook my flight?  ",
            "promptTranslation": "Giup toi dat lai chuyen bay?",
            "sampleResponse": "  Certainly, let me check the next available seat.  ",
            "responseTranslation": "Chac chan roi.",
            "cefrLevel": "B2",
            "register": "Formal",
            "examTarget": "Toeic"
          },
          {
            "category": "  Workplace & Office  ",
            "promptSentence": "  Can we touch base later? ",
            "sampleResponse": "  Sure, 3 PM works for me. "
          },
          {
            "category": "Workplace & Office",
            "promptSentence": "can we touch base later?",
            "sampleResponse": "Duplicate in same batch"
          }
        ]
        """;

        var added = new List<ConversationPhrase>();
        _repoMock.Setup(r => r.AddAsync(It.IsAny<ConversationPhrase>(), It.IsAny<CancellationToken>()))
            .Callback<ConversationPhrase, CancellationToken>((p, _) => added.Add(p))
            .ReturnsAsync((ConversationPhrase p, CancellationToken _) => p);

        var sut = CreateSut();

        // Act (Skip existing DB item, deduplicate intra-batch duplicate)
        var skipResult = await sut.ImportSmartAsync(json, "phrases.json", ImportConflictResolution.Skip);

        // Assert Skip
        skipResult.TotalImported.Should().Be(1);
        skipResult.TotalSkipped.Should().Be(2);
        added.Should().ContainSingle();
        added[0].Category.Should().Be("Workplace & Office");
        added[0].PromptSentence.Should().Be("Can we touch base later?");
        added[0].SampleResponse.Should().Be("Sure, 3 PM works for me.");

        // Act (Overwrite existing DB item)
        var overwriteResult = await sut.ImportSmartAsync(json, "phrases.json", ImportConflictResolution.Overwrite);
        overwriteResult.TotalUpdated.Should().Be(1);
        existing.SampleResponse.Should().Be("Certainly, let me check the next available seat.");
        existing.Register.Should().Be(PhraseRegister.Formal);
        existing.ExamTarget.Should().Be(ExamTarget.Toeic);
    }

    [Fact]
    public async Task ExportToFileAsync_PathTraversalAttempt_ShouldThrowSecurityException()
    {
        // Arrange
        var allowedDir = Path.Combine(Path.GetTempPath(), $"lexora_phrase_safe_{Guid.NewGuid():N}");
        var escapedPath = Path.Combine(allowedDir, "..", "outside_evil.json");
        var sut = CreateSut();

        // Act
        Func<Task> act = async () => await sut.ExportToFileAsync(
            escapedPath, allowedDir, DataFormat.Json);

        // Assert
        await act.Should().ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task SubmitPhraseReviewAsync_RepeatedAgainRatings_ShouldKeepEaseFactorAtLeast1Point3AndSyncStudyLog()
    {
        // Arrange
        var phrase = new ConversationPhrase
        {
            Category = "Business & TOEIC",
            PromptSentence = "Could we postpone tomorrow's briefing?",
            PromptTranslation = "Chung ta hoan cuoc hop ngay mai duoc khong?",
            SampleResponse = "Of course, let's move it to Thursday morning.",
            ResponseTranslation = "Duoc chu, doi sang sang thu Nam nhe.",
            EaseFactor = 1.45,
            Repetitions = 3,
            IntervalDays = 10,
            Status = LearningStatus.Remembered
        };
        phrase.UpdateNormalizedFields();

        _repoMock.Setup(r => r.GetByIdAsync(phrase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(phrase);
        _vocabRepoMock.Setup(v => v.GetByTermAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);
        _vocabRepoMock.Setup(v => v.AddAsync(It.IsAny<Word>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word w, CancellationToken _) => w);

        var studyLogs = new List<StudyLog>();
        _reviewRepoMock.Setup(r => r.AddStudyLog(It.IsAny<StudyLog>()))
            .Callback<StudyLog>(l => studyLogs.Add(l));

        var sut = CreateSut(withReviewSync: true);

        // Act: submit Again twice to verify EF floor >= 1.3 and StudyLog sync
        var r1 = await sut.SubmitPhraseReviewAsync(phrase.Id, ReviewRating.Again, responseTimeMs: 1200);
        var r2 = await sut.SubmitPhraseReviewAsync(phrase.Id, ReviewRating.Again, responseTimeMs: 900);

        // Assert
        r1.NewEaseFactor.Should().BeGreaterThanOrEqualTo(1.3);
        r2.NewEaseFactor.Should().BeGreaterThanOrEqualTo(1.3);
        r2.SyncedStudyLog.Should().BeTrue();
        phrase.EaseFactor.Should().BeGreaterThanOrEqualTo(1.3);
        phrase.Lapses.Should().BeGreaterThanOrEqualTo(2);
        phrase.LinkedWordId.Should().NotBeNull();
        studyLogs.Should().HaveCount(2);
        _reviewRepoMock.Verify(r => r.UpdateStreakAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EnqueueForSpacedRepetitionAsync_ShouldInitializeScheduleAndCommitAtomically()
    {
        // Arrange
        var phrase = new ConversationPhrase
        {
            PromptSentence = "How's everything coming along?",
            SampleResponse = "Everything is right on track.",
            Status = LearningStatus.New,
            EaseFactor = 2.5
        };
        phrase.UpdateNormalizedFields();

        _repoMock.Setup(r => r.GetByIdAsync(phrase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(phrase);

        var sut = CreateSut(withReviewSync: false);

        // Act
        var dto = await sut.EnqueueForSpacedRepetitionAsync(phrase.Id);

        // Assert
        dto.Status.Should().Be(LearningStatus.Learning);
        dto.NextReviewAt.Should().NotBeNull();
        dto.EaseFactor.Should().BeGreaterThanOrEqualTo(1.3);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
