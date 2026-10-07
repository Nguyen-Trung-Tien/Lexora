using FluentAssertions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Application.Services;
using Lexora.Application.SpacedRepetition;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Moq;
using Xunit;

namespace Lexora.Tests.Unit;

public class LearningStorageServiceTests
{
    private readonly Mock<ILearningStorageRepository> _mockStorageRepo;
    private readonly Mock<IVocabularyRepository> _mockVocabRepo;
    private readonly Mock<IGrammarRepository> _mockGrammarRepo;
    private readonly Mock<IReviewRepository> _mockReviewRepo;
    private readonly ISpacedRepetitionEngine _srEngine;
    private readonly Mock<ITestService> _mockTestService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly LearningStorageService _service;

    public LearningStorageServiceTests()
    {
        _mockStorageRepo = new Mock<ILearningStorageRepository>();
        _mockVocabRepo = new Mock<IVocabularyRepository>();
        _mockGrammarRepo = new Mock<IGrammarRepository>();
        _mockReviewRepo = new Mock<IReviewRepository>();
        _srEngine = new Sm2SpacedRepetitionEngine();
        _mockTestService = new Mock<ITestService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new LearningStorageService(
            _mockStorageRepo.Object,
            _mockVocabRepo.Object,
            _mockGrammarRepo.Object,
            _mockReviewRepo.Object,
            _srEngine,
            _mockTestService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task MoveWordToStorageAsync_WhenItemDoesNotExist_ShouldCreateNewItem()
    {
        // Arrange
        var wordId = Guid.NewGuid();
        var word = new Word
        {
            Id = wordId,
            Term = "resilient",
            PhoneticUs = "/rɪˈzɪl.jənt/",
            PrimaryPartOfSpeech = PartOfSpeech.Adjective,
            Level = CefrLevel.B2,
            Meanings = new List<WordMeaning>
            {
                new WordMeaning { DefinitionVi = "kiên cường, mau phục hồi", DefinitionEn = "able to withstand or recover quickly" }
            }
        };

        _mockStorageRepo.Setup(r => r.GetByWordIdAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningStorageItem?)null);
        _mockVocabRepo.Setup(r => r.GetWithDetailsAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(word);

        LearningStorageItem? capturedItem = null;
        _mockStorageRepo.Setup(r => r.AddAsync(It.IsAny<LearningStorageItem>(), It.IsAny<CancellationToken>()))
            .Callback<LearningStorageItem, CancellationToken>((item, _) =>
            {
                capturedItem = item;
                item.Word = word;
            })
            .ReturnsAsync((LearningStorageItem item, CancellationToken _) => item);

        _mockStorageRepo.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturedItem);

        // Act
        var result = await _service.MoveWordToStorageAsync(wordId, StorageAdditionReason.Manual, "Difficult to remember");

        // Assert
        result.Should().NotBeNull();
        result.WordId.Should().Be(wordId);
        result.Title.Should().Be("resilient");
        result.ItemType.Should().Be(StorageItemType.Vocabulary);
        result.Status.Should().Be(StorageLearningStatus.Learning);
        result.Note.Should().Be("Difficult to remember");
        capturedItem.Should().NotBeNull();
        capturedItem!.ReasonAdded.Should().Be(StorageAdditionReason.Manual);
        _mockStorageRepo.Verify(r => r.AddAsync(It.IsAny<LearningStorageItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveWordToStorageAsync_WhenItemAlreadyExists_ShouldUpdateStatusAndNote()
    {
        // Arrange
        var wordId = Guid.NewGuid();
        var word = new Word { Id = wordId, Term = "tenacious", PrimaryPartOfSpeech = PartOfSpeech.Adjective, Level = CefrLevel.C1 };
        var existingItem = new LearningStorageItem
        {
            Id = Guid.NewGuid(),
            WordId = wordId,
            Word = word,
            ItemType = StorageItemType.Vocabulary,
            Status = StorageLearningStatus.Mastered,
            ReasonAdded = StorageAdditionReason.Manual,
            Note = "Old note"
        };

        _mockStorageRepo.Setup(r => r.GetByWordIdAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingItem);

        // Act
        var result = await _service.MoveWordToStorageAsync(wordId, StorageAdditionReason.MultipleErrors, "Failed 3 times");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(StorageLearningStatus.Weak);
        result.Note.Should().Be("Failed 3 times");
        existingItem.ReasonAdded.Should().Be(StorageAdditionReason.MultipleErrors);
        _mockStorageRepo.Verify(r => r.UpdateAsync(existingItem, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveGrammarToStorageAsync_WhenItemDoesNotExist_ShouldCreateNewItem()
    {
        // Arrange
        var grammarId = Guid.NewGuid();
        var grammarRule = new GrammarRule
        {
            Id = grammarId,
            Title = "Inversion with negative adverbs",
            ExplanationVi = "Đảo ngữ với phó từ phủ định",
            Structure = "Hardly / Scarcely + had + S + V3/ed + when + S + V2/ed",
            Level = CefrLevel.B2
        };

        _mockStorageRepo.Setup(r => r.GetByGrammarRuleIdAsync(grammarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningStorageItem?)null);
        _mockGrammarRepo.Setup(r => r.GetWithExamplesAsync(grammarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grammarRule);

        LearningStorageItem? capturedItem = null;
        _mockStorageRepo.Setup(r => r.AddAsync(It.IsAny<LearningStorageItem>(), It.IsAny<CancellationToken>()))
            .Callback<LearningStorageItem, CancellationToken>((item, _) =>
            {
                capturedItem = item;
                item.GrammarRule = grammarRule;
            })
            .ReturnsAsync((LearningStorageItem item, CancellationToken _) => item);

        _mockStorageRepo.Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturedItem);

        // Act
        var result = await _service.MoveGrammarToStorageAsync(grammarId, StorageAdditionReason.Manual);

        // Assert
        result.Should().NotBeNull();
        result.GrammarRuleId.Should().Be(grammarId);
        result.Title.Should().Be("Inversion with negative adverbs");
        result.ItemType.Should().Be(StorageItemType.Grammar);
        result.Status.Should().Be(StorageLearningStatus.Learning);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsMasteredAsync_ShouldSetStatusToMasteredAndReduceFrequency()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = new LearningStorageItem
        {
            Id = itemId,
            ItemType = StorageItemType.Vocabulary,
            Status = StorageLearningStatus.Weak,
            IntervalDays = 1,
            EaseFactor = 1.8,
            Repetitions = 2
        };

        _mockStorageRepo.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        await _service.MarkAsMasteredAsync(itemId);

        // Assert
        item.Status.Should().Be(StorageLearningStatus.Mastered);
        item.IntervalDays.Should().BeGreaterThan(10);
        item.RetentionProbability.Should().Be(95.0);
        item.Repetitions.Should().BeGreaterThanOrEqualTo(5);
        _mockStorageRepo.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveFromStorageAsync_ShouldCallDeleteAndSave()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = new LearningStorageItem { Id = itemId };

        _mockStorageRepo.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Act
        await _service.RemoveFromStorageAsync(itemId);

        // Assert
        _mockStorageRepo.Verify(r => r.DeleteAsync(item, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoCheckAndAddToStorageAsync_WhenErrorsExceedThreshold_ShouldAddToStorage()
    {
        // Arrange
        var wordId = Guid.NewGuid();
        var word = new Word { Id = wordId, Term = "ubiquitous" };

        _mockStorageRepo.Setup(r => r.GetByWordIdAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningStorageItem?)null);
        _mockVocabRepo.Setup(r => r.GetWithDetailsAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(word);

        // Act - 3 consecutive errors
        await _service.AutoCheckAndAddToStorageAsync(wordId, consecutiveErrors: 3, retentionRate: 0.8);

        // Assert
        _mockStorageRepo.Verify(r => r.AddAsync(It.Is<LearningStorageItem>(i =>
            i.WordId == wordId &&
            i.Status == StorageLearningStatus.Weak &&
            i.ReasonAdded == StorageAdditionReason.MultipleErrors), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoCheckAndAddToStorageAsync_WhenRetentionLow_ShouldAddToStorage()
    {
        // Arrange
        var wordId = Guid.NewGuid();
        var word = new Word { Id = wordId, Term = "ephemeral" };

        _mockStorageRepo.Setup(r => r.GetByWordIdAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningStorageItem?)null);
        _mockVocabRepo.Setup(r => r.GetWithDetailsAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(word);

        // Act - low retention (< 50%)
        await _service.AutoCheckAndAddToStorageAsync(wordId, consecutiveErrors: 1, retentionRate: 0.35);

        // Assert
        _mockStorageRepo.Verify(r => r.AddAsync(It.Is<LearningStorageItem>(i =>
            i.WordId == wordId &&
            i.ReasonAdded == StorageAdditionReason.LowRetention), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GeneratePracticeAsync_ForVocabulary_ShouldGenerateMCQsWithOptions()
    {
        // Arrange
        var word1 = new Word
        {
            Id = Guid.NewGuid(),
            Term = "meticulous",
            Meanings = new List<WordMeaning> { new WordMeaning { DefinitionVi = "tỉ mỉ, cẩn thận" } }
        };
        var word2 = new Word
        {
            Id = Guid.NewGuid(),
            Term = "pragmatic",
            Meanings = new List<WordMeaning> { new WordMeaning { DefinitionVi = "thực tế, thực dụng" } }
        };

        var items = new List<LearningStorageItem>
        {
            new LearningStorageItem
            {
                Id = Guid.NewGuid(),
                WordId = word1.Id,
                Word = word1,
                ItemType = StorageItemType.Vocabulary,
                Status = StorageLearningStatus.Weak
            },
            new LearningStorageItem
            {
                Id = Guid.NewGuid(),
                WordId = word2.Id,
                Word = word2,
                ItemType = StorageItemType.Vocabulary,
                Status = StorageLearningStatus.Learning
            }
        };

        _mockStorageRepo.Setup(r => r.GetItemsForPracticeAsync(StorageItemType.Vocabulary, 10, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        // Act
        var questions = await _service.GeneratePracticeAsync(StorageItemType.Vocabulary, limit: 10, prioritizeWeak: true);

        // Assert
        questions.Should().NotBeEmpty();
        questions.Count.Should().Be(2);
        questions.All(q => q.Options.Count >= 2).Should().BeTrue();
        questions.All(q => !string.IsNullOrEmpty(q.CorrectAnswer)).Should().BeTrue();
    }

    [Fact]
    public async Task EvaluatePracticeAsync_WhenAnswerIsCorrect_ShouldUpdateSM2AndInterval()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var word = new Word { Id = Guid.NewGuid(), Term = "eloquent" };
        var storageItem = new LearningStorageItem
        {
            Id = itemId,
            WordId = word.Id,
            Word = word,
            ItemType = StorageItemType.Vocabulary,
            Status = StorageLearningStatus.Learning,
            IntervalDays = 1,
            EaseFactor = 2.5,
            Repetitions = 1,
            CorrectCount = 1,
            IncorrectCount = 1
        };

        _mockStorageRepo.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageItem);

        var questions = new List<StoragePracticeQuestionDto>
        {
            new StoragePracticeQuestionDto(
                StorageItemId: itemId,
                ItemType: StorageItemType.Vocabulary,
                ReferenceId: word.Id,
                QuestionNumber: 1,
                Prompt: "Nghĩa của từ 'eloquent'?",
                SubPrompt: "/ˈel.ə.kwənt/",
                AudioPath: null,
                CorrectAnswer: "hùng biện, lưu loát",
                Options: new List<string> { "hùng biện, lưu loát", "vụng về", "chậm chạp", "lười biếng" },
                Explanation: "Test explanation")
        };

        var answers = new List<StoragePracticeSubmissionDto>
        {
            new StoragePracticeSubmissionDto(StorageItemId: itemId, SelectedAnswer: "hùng biện, lưu loát")
        };

        // Act
        var result = await _service.EvaluatePracticeAsync(questions, answers);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(1);
        result.Correct.Should().Be(1);
        result.ScorePercent.Should().Be(100);
        storageItem.CorrectCount.Should().Be(2);
        storageItem.Repetitions.Should().Be(2);
        storageItem.IntervalDays.Should().BeGreaterThan(1);
        storageItem.EaseFactor.Should().BeGreaterThanOrEqualTo(1.3);
        _mockStorageRepo.Verify(r => r.UpdateAsync(storageItem, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluatePracticeAsync_WhenAnswerIsIncorrect_ShouldResetIntervalAndSetStatusToWeak()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var word = new Word { Id = Guid.NewGuid(), Term = "ephemeral" };
        var storageItem = new LearningStorageItem
        {
            Id = itemId,
            WordId = word.Id,
            Word = word,
            ItemType = StorageItemType.Vocabulary,
            Status = StorageLearningStatus.Learning,
            IntervalDays = 6,
            EaseFactor = 2.5,
            Repetitions = 2,
            CorrectCount = 4,
            IncorrectCount = 1
        };

        _mockStorageRepo.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageItem);

        var questions = new List<StoragePracticeQuestionDto>
        {
            new StoragePracticeQuestionDto(
                StorageItemId: itemId,
                ItemType: StorageItemType.Vocabulary,
                ReferenceId: word.Id,
                QuestionNumber: 1,
                Prompt: "Nghĩa của từ 'ephemeral'?",
                SubPrompt: null,
                AudioPath: null,
                CorrectAnswer: "phù du, chóng tàn",
                Options: new List<string> { "phù du, chóng tàn", "bền vững", "vĩnh cửu", "to lớn" },
                Explanation: "Test explanation")
        };

        var answers = new List<StoragePracticeSubmissionDto>
        {
            new StoragePracticeSubmissionDto(StorageItemId: itemId, SelectedAnswer: "vĩnh cửu") // Wrong!
        };

        // Act
        var result = await _service.EvaluatePracticeAsync(questions, answers);

        // Assert
        result.Total.Should().Be(1);
        result.Correct.Should().Be(0);
        result.ScorePercent.Should().Be(0);
        storageItem.Repetitions.Should().Be(0);
        storageItem.IntervalDays.Should().Be(1); // Lapsed back to 1 day
        storageItem.Lapses.Should().Be(1);
        storageItem.Status.Should().Be(StorageLearningStatus.Weak);
        storageItem.EaseFactor.Should().BeGreaterThanOrEqualTo(1.3); // SM-2 EF floor invariant
    }

    [Fact]
    public async Task RemoveRangeFromStorageAsync_WhenIdsEmpty_ShouldReturnZeroWithoutStartingTransaction()
    {
        // Act
        var result = await _service.RemoveRangeFromStorageAsync(Array.Empty<Guid>());

        // Assert
        result.Should().Be(0);
        _mockStorageRepo.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveRangeFromStorageAsync_WhenValidIdsProvided_ShouldDeleteAndCommit()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        _mockStorageRepo
            .Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _service.RemoveRangeFromStorageAsync(ids);

        // Assert
        result.Should().Be(3);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.ClearTracker(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRangeFromStorageAsync_WhenExceptionOccurs_ShouldRollbackTransaction()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        _mockStorageRepo
            .Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        var act = () => _service.RemoveRangeFromStorageAsync(ids);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveWordsToStorageAsync_WhenMultipleWordsProvided_ShouldBatchAddAndCommitOnce()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var word1 = new Word { Id = id1, Term = "word1" };
        var word2 = new Word { Id = id2, Term = "word2" };

        _mockStorageRepo.Setup(r => r.GetByWordIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningStorageItem?)null);
        _mockVocabRepo.Setup(r => r.GetWithDetailsAsync(id1, It.IsAny<CancellationToken>())).ReturnsAsync(word1);
        _mockVocabRepo.Setup(r => r.GetWithDetailsAsync(id2, It.IsAny<CancellationToken>())).ReturnsAsync(word2);

        // Act
        var added = await _service.MoveWordsToStorageAsync(new[] { id1, id2 }, StorageAdditionReason.Manual, "Batch note");

        // Assert
        added.Should().Be(2);
        _mockStorageRepo.Verify(r => r.AddAsync(It.IsAny<LearningStorageItem>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveWordsToStorageAsync_WhenWordDoesNotExist_ShouldSkipGracefullyWithoutCrashing()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _mockStorageRepo.Setup(r => r.GetByWordIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningStorageItem?)null);
        _mockVocabRepo.Setup(r => r.GetWithDetailsAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Word?)null);

        // Act
        var added = await _service.MoveWordsToStorageAsync(new[] { missingId });

        // Assert
        added.Should().Be(0);
        _mockStorageRepo.Verify(r => r.AddAsync(It.IsAny<LearningStorageItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveGrammarsToStorageAsync_WhenMultipleRulesProvided_ShouldBatchAddAndCommitOnce()
    {
        // Arrange
        var gId1 = Guid.NewGuid();
        var gId2 = Guid.NewGuid();
        var rule1 = new GrammarRule { Id = gId1, Title = "Rule 1", Structure = "S + V" };
        var rule2 = new GrammarRule { Id = gId2, Title = "Rule 2", Structure = "S + to be" };

        _mockStorageRepo.Setup(r => r.GetByGrammarRuleIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningStorageItem?)null);
        _mockGrammarRepo.Setup(r => r.GetWithExamplesAsync(gId1, It.IsAny<CancellationToken>())).ReturnsAsync(rule1);
        _mockGrammarRepo.Setup(r => r.GetWithExamplesAsync(gId2, It.IsAny<CancellationToken>())).ReturnsAsync(rule2);

        // Act
        var added = await _service.MoveGrammarsToStorageAsync(new[] { gId1, gId2 }, StorageAdditionReason.Manual, "Grammar note");

        // Assert
        added.Should().Be(2);
        _mockStorageRepo.Verify(r => r.AddAsync(It.IsAny<LearningStorageItem>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStoredWordIdsAsync_ShouldReturnIdsFromRepository()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var expectedSet = new HashSet<Guid> { id1, id2 };
        _mockStorageRepo.Setup(r => r.GetStoredWordIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSet);

        // Act
        var result = await _service.GetStoredWordIdsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedSet);
        _mockStorageRepo.Verify(r => r.GetStoredWordIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStoredGrammarRuleIdsAsync_ShouldReturnIdsFromRepository()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var expectedSet = new HashSet<Guid> { id1 };
        _mockStorageRepo.Setup(r => r.GetStoredGrammarRuleIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSet);

        // Act
        var result = await _service.GetStoredGrammarRuleIdsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedSet);
        _mockStorageRepo.Verify(r => r.GetStoredGrammarRuleIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void NotifyGlobalStorageChanged_ShouldInvokeRegisteredSubscribers()
    {
        // Arrange
        bool invoked = false;
        void Handler(object? sender, EventArgs e) => invoked = true;

        LearningStorageService.GlobalStorageChanged += Handler;

        try
        {
            // Act
            LearningStorageService.NotifyGlobalStorageChanged();

            // Assert
            invoked.Should().BeTrue();
        }
        finally
        {
            LearningStorageService.GlobalStorageChanged -= Handler;
        }
    }

    [Fact]
    public void NotifyGlobalStorageChanged_AfterUnsubscribe_ShouldNotInvokeSubscriber()
    {
        // Arrange
        int callCount = 0;
        void Handler(object? sender, EventArgs e) => callCount++;

        LearningStorageService.GlobalStorageChanged += Handler;
        LearningStorageService.GlobalStorageChanged -= Handler;

        // Act
        LearningStorageService.NotifyGlobalStorageChanged();

        // Assert
        callCount.Should().Be(0);
    }
}
