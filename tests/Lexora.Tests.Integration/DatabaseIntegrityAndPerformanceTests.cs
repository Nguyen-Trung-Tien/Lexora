using System.Diagnostics;
using FluentAssertions;
using Lexora.Application.Services;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Infrastructure.Data;
using Lexora.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Integration;

public class DatabaseIntegrityAndPerformanceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly VocabularyRepository _vocabRepo;
    private readonly UnitOfWork _unitOfWork;

    public DatabaseIntegrityAndPerformanceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);
        _context.Database.EnsureCreated();

        _vocabRepo = new VocabularyRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task CascadeDelete_ShouldRemoveMeaningsExamplesAndReviewCards_WhenWordDeleted()
    {
        // Arrange - Create word with meanings, examples, and review card
        var word = new Word
        {
            Term = "integrity",
            PhoneticUk = "/ɪnˈteɡ.rə.ti/",
            PhoneticUs = "/ɪnˈteɡ.rə.t̬i/",
            Level = CefrLevel.C1
        };

        var meaning = new WordMeaning
        {
            PartOfSpeech = PartOfSpeech.Noun,
            DefinitionEn = "The quality of being honest and having strong moral principles.",
            DefinitionVi = "Tính chính trực, liêm chính."
        };

        var example = new ExampleSentence
        {
            SentenceEn = "A leader must demonstrate the highest levels of integrity.",
            SentenceVi = "Một nhà lãnh đạo phải thể hiện mức độ chính trực cao nhất."
        };

        meaning.Examples.Add(example);
        word.Meanings.Add(meaning);

        await _vocabRepo.AddAsync(word);
        await _unitOfWork.SaveChangesAsync();

        var card = new ReviewCard
        {
            WordId = word.Id,
            Repetitions = 1,
            IntervalDays = 1,
            EaseFactor = 2.5f,
            NextReviewAt = DateTime.UtcNow
        };
        _context.ReviewCards.Add(card);
        await _unitOfWork.SaveChangesAsync();

        // Verify entities exist
        (await _context.Words.CountAsync()).Should().Be(1);
        (await _context.WordMeanings.CountAsync()).Should().Be(1);
        (await _context.ExampleSentences.CountAsync()).Should().Be(1);
        (await _context.ReviewCards.CountAsync()).Should().Be(1);

        // Act - Delete word
        await _vocabRepo.DeleteAsync(word);
        await _unitOfWork.SaveChangesAsync();

        // Assert - Cascaded cleanup
        (await _context.Words.CountAsync()).Should().Be(0);
        (await _context.WordMeanings.CountAsync()).Should().Be(0);
        (await _context.ExampleSentences.CountAsync()).Should().Be(0);
        (await _context.ReviewCards.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LargeVolume_SearchAndPagination_ShouldPerformUnderThreshold()
    {
        // Arrange - Bulk insert 300 words
        var words = new List<Word>(300);
        for (int i = 1; i <= 300; i++)
        {
            var word = new Word
            {
                Term = $"benchmark_word_{i:D4}",
                PhoneticUk = $"/wɜːd_{i}/",
                PhoneticUs = $"/wɜːrd_{i}/",
                Level = (CefrLevel)(i % 6)
            };

            var meaning = new WordMeaning
            {
                PartOfSpeech = (PartOfSpeech)(i % 5),
                DefinitionEn = $"Definition test meaning for word number {i}",
                DefinitionVi = $"Nghĩa thử nghiệm số {i}"
            };

            meaning.Examples.Add(new ExampleSentence
            {
                SentenceEn = $"Sample sentence for benchmark number {i}.",
                SentenceVi = $"Câu ví dụ chuẩn đo kiểm số {i}."
            });

            word.Meanings.Add(meaning);
            words.Add(word);
        }

        await _context.Words.AddRangeAsync(words);
        await _context.SaveChangesAsync();

        // Act & Measure - Search with filter & paging
        var sw = Stopwatch.StartNew();
        var results = await _vocabRepo.SearchAsync(
            searchTerm: "benchmark_word_01",
            level: CefrLevel.B1,
            skip: 0,
            take: 20);
        sw.Stop();

        // Assert
        results.Should().NotBeNull();
        sw.ElapsedMilliseconds.Should().BeLessThan(500, "Database queries must execute within 500ms");
    }

    [Fact]
    public async Task CancellationToken_ShouldCancelExecution_WhenRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-canceled token

        // Act
        var act = async () => await _vocabRepo.SearchAsync(searchTerm: "anything", cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
