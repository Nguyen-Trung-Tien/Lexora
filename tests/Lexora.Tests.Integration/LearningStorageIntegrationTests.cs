using FluentAssertions;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Infrastructure.Data;
using Lexora.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Integration;

public class LearningStorageIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly LearningStorageRepository _storageRepo;
    private readonly VocabularyRepository _vocabRepo;
    private readonly GrammarRepository _grammarRepo;
    private readonly UnitOfWork _unitOfWork;

    public LearningStorageIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);

        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _storageRepo = new LearningStorageRepository(_context);
        _vocabRepo = new VocabularyRepository(_context);
        _grammarRepo = new GrammarRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task Initializer_ShouldCreateLearningStorageItemsTable()
    {
        // Assert - table exists and can be queried
        var count = await _context.LearningStorageItems.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task StorageRepository_AddAndRetrieve_VocabularyItem_ShouldIncludeWordDetails()
    {
        // Arrange - get a seeded word
        var word = await _context.Words.Include(w => w.Meanings).FirstAsync();

        var storageItem = new LearningStorageItem
        {
            Id = Guid.NewGuid(),
            ItemType = StorageItemType.Vocabulary,
            WordId = word.Id,
            Status = StorageLearningStatus.Weak,
            ReasonAdded = StorageAdditionReason.MultipleErrors,
            Note = "Hard to remember definition",
            IntervalDays = 1,
            EaseFactor = 1.9,
            Repetitions = 0,
            NextReviewAt = DateTime.UtcNow
        };

        // Act
        await _storageRepo.AddAsync(storageItem);
        await _unitOfWork.SaveChangesAsync();

        var retrieved = await _storageRepo.GetWithDetailsAsync(storageItem.Id);
        var byWord = await _storageRepo.GetByWordIdAsync(word.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.WordId.Should().Be(word.Id);
        retrieved.Word.Should().NotBeNull();
        retrieved.Word!.Term.Should().Be(word.Term);
        retrieved.Word.Meanings.Should().NotBeEmpty();

        byWord.Should().NotBeNull();
        byWord!.Id.Should().Be(storageItem.Id);
    }

    [Fact]
    public async Task StorageRepository_AddAndRetrieve_GrammarItem_ShouldIncludeRuleDetails()
    {
        // Arrange - get a seeded grammar rule
        var grammar = await _context.GrammarRules.FirstAsync();

        var storageItem = new LearningStorageItem
        {
            Id = Guid.NewGuid(),
            ItemType = StorageItemType.Grammar,
            GrammarRuleId = grammar.Id,
            Status = StorageLearningStatus.Learning,
            ReasonAdded = StorageAdditionReason.Manual,
            Note = "Confusing structure",
            IntervalDays = 2,
            EaseFactor = 2.4,
            Repetitions = 1,
            NextReviewAt = DateTime.UtcNow.AddDays(2)
        };

        // Act
        await _storageRepo.AddAsync(storageItem);
        await _unitOfWork.SaveChangesAsync();

        var retrieved = await _storageRepo.GetByGrammarRuleIdAsync(grammar.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.GrammarRuleId.Should().Be(grammar.Id);
        retrieved.GrammarRule.Should().NotBeNull();
        retrieved.GrammarRule!.Title.Should().Be(grammar.Title);
    }

    [Fact]
    public async Task StorageRepository_FilteringAndPagination_ShouldFilterCorrectly()
    {
        // Arrange - add words to storage
        var words = await _context.Words.Take(5).ToListAsync();
        for (int i = 0; i < words.Count; i++)
        {
            var status = i switch
            {
                0 => StorageLearningStatus.Weak,
                1 => StorageLearningStatus.Weak,
                2 => StorageLearningStatus.Learning,
                _ => StorageLearningStatus.Mastered
            };

            await _storageRepo.AddAsync(new LearningStorageItem
            {
                Id = Guid.NewGuid(),
                ItemType = StorageItemType.Vocabulary,
                WordId = words[i].Id,
                Status = status,
                ReasonAdded = StorageAdditionReason.Manual,
                NextReviewAt = i == 0 ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(5)
            });
        }
        await _unitOfWork.SaveChangesAsync();

        // Act & Assert 1: Filter by Status = Weak
        var weakItems = await _storageRepo.GetFilteredPagedAsync(
            itemType: StorageItemType.Vocabulary,
            status: StorageLearningStatus.Weak,
            search: null,
            dueTodayOnly: null,
            skip: 0,
            take: 10);
        weakItems.Should().HaveCount(2);

        // Act & Assert 2: Filter by DueTodayOnly
        var dueTodayItems = await _storageRepo.GetFilteredPagedAsync(
            itemType: null,
            status: null,
            search: null,
            dueTodayOnly: true,
            skip: 0,
            take: 10);
        dueTodayItems.Should().HaveCount(1);

        // Act & Assert 3: Total count
        var totalCount = await _storageRepo.CountFilteredAsync(
            itemType: StorageItemType.Vocabulary,
            status: null,
            search: null,
            dueTodayOnly: null);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task StorageRepository_GetStatsAsync_ShouldComputeAccurateAggregates()
    {
        // Arrange
        var words = await _context.Words.Take(3).ToListAsync();
        var grammar = await _context.GrammarRules.FirstAsync();

        await _storageRepo.AddAsync(new LearningStorageItem
        {
            Id = Guid.NewGuid(),
            ItemType = StorageItemType.Vocabulary,
            WordId = words[0].Id,
            Status = StorageLearningStatus.Weak,
            IncorrectCount = 7,
            CorrectCount = 3,
            RetentionProbability = 30.0,
            NextReviewAt = DateTime.UtcNow.AddDays(-1)
        });

        await _storageRepo.AddAsync(new LearningStorageItem
        {
            Id = Guid.NewGuid(),
            ItemType = StorageItemType.Vocabulary,
            WordId = words[1].Id,
            Status = StorageLearningStatus.Learning,
            IncorrectCount = 1,
            CorrectCount = 4,
            RetentionProbability = 80.0,
            NextReviewAt = DateTime.UtcNow.AddDays(3)
        });

        await _storageRepo.AddAsync(new LearningStorageItem
        {
            Id = Guid.NewGuid(),
            ItemType = StorageItemType.Grammar,
            GrammarRuleId = grammar.Id,
            Status = StorageLearningStatus.Mastered,
            IncorrectCount = 1,
            CorrectCount = 19,
            RetentionProbability = 95.0,
            NextReviewAt = DateTime.UtcNow.AddDays(15)
        });

        await _unitOfWork.SaveChangesAsync();

        // Act
        var stats = await _storageRepo.GetStatsAsync();

        // Assert
        stats.Total.Should().Be(3);
        stats.VocabCount.Should().Be(2);
        stats.GrammarCount.Should().Be(1);
        stats.WeakCount.Should().Be(1);
        stats.LearningCount.Should().Be(1);
        stats.MasteredCount.Should().Be(1);
        stats.DueTodayCount.Should().Be(1);
        stats.AvgRetention.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CascadeDelete_WhenWordIsDeleted_ShouldCascadeDeleteStorageItem()
    {
        // Arrange
        var word = await _context.Words.FirstAsync();
        var storageItem = new LearningStorageItem
        {
            Id = Guid.NewGuid(),
            ItemType = StorageItemType.Vocabulary,
            WordId = word.Id,
            Status = StorageLearningStatus.Weak
        };

        await _storageRepo.AddAsync(storageItem);
        await _unitOfWork.SaveChangesAsync();

        // Act - Delete word
        _context.Words.Remove(word);
        await _context.SaveChangesAsync();

        // Assert - Storage item should be deleted
        var itemAfterDelete = await _context.LearningStorageItems.FindAsync(storageItem.Id);
        itemAfterDelete.Should().BeNull();
    }

    [Fact]
    public async Task MoveWordAndGrammarToStorage_ShouldNotThrow()
    {
        var word = await _context.Words.FirstAsync();
        var grammar = await _context.GrammarRules.FirstAsync();

        var service = new Lexora.Application.Services.LearningStorageService(
            _storageRepo,
            _vocabRepo,
            _grammarRepo,
            new ReviewRepository(_context),
            new Lexora.Application.SpacedRepetition.Sm2SpacedRepetitionEngine(),
            null!,
            _unitOfWork
        );

        var wordItem = await service.MoveWordToStorageAsync(word.Id, StorageAdditionReason.Manual, "Test note");
        wordItem.Should().NotBeNull();
        wordItem.Title.Should().Be(word.Term);

        var grammarItem = await service.MoveGrammarToStorageAsync(grammar.Id, StorageAdditionReason.Manual, "Grammar note");
        grammarItem.Should().NotBeNull();
        grammarItem.Title.Should().Be(grammar.Title);

        var pagedVocab = await service.GetPagedItemsAsync(StorageItemType.Vocabulary);
        pagedVocab.Items.Should().NotBeEmpty();

        var pagedGrammar = await service.GetPagedItemsAsync(StorageItemType.Grammar);
        pagedGrammar.Items.Should().NotBeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
