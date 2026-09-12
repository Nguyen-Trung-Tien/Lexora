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

public class SqliteDatabaseTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;

    public SqliteDatabaseTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);
    }

    [Fact]
    public async Task Initializer_ShouldCreateTablesAndSeedStarterPackData()
    {
        // Arrange
        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);

        // Act
        await initializer.InitializeAsync();

        // Assert
        var wordsCount = await _context.Words.CountAsync();
        wordsCount.Should().Be(10);

        var topicsCount = await _context.Topics.CountAsync();
        topicsCount.Should().Be(3);

        var examTagsCount = await _context.ExamTags.CountAsync();
        examTagsCount.Should().Be(3);

        var grammarRulesCount = await _context.GrammarRules.CountAsync();
        grammarRulesCount.Should().Be(2);

        var streak = await _context.UserStreaks.FirstOrDefaultAsync();
        streak.Should().NotBeNull();
        streak!.CurrentStreak.Should().Be(1);
    }

    [Fact]
    public async Task VocabularyRepository_SearchAsync_ShouldFilterByTermAndLevel()
    {
        // Arrange
        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        await initializer.InitializeAsync();
        var repo = new VocabularyRepository(_context);

        // Act - Search for "collaborate"
        var searchResult = await repo.SearchAsync(searchTerm: "collaborate");

        // Assert
        searchResult.Should().HaveCount(1);
        searchResult[0].Term.Should().Be("collaborate");
        searchResult[0].Meanings.Should().NotBeEmpty();
        searchResult[0].Meanings.First().Examples.Should().NotBeEmpty();

        // Act - Search by level C1
        var c1Words = await repo.SearchAsync(level: CefrLevel.C1);
        c1Words.Should().HaveCount(4); // resilient, ubiquitous, meticulous, streamline
    }

    [Fact]
    public async Task ReviewRepository_StreakManagement_ShouldIncrementCorrectly()
    {
        // Arrange
        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        await initializer.InitializeAsync();
        var repo = new ReviewRepository(_context);
        var unitOfWork = new UnitOfWork(_context);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);

        // Act - Update streak for consecutive day
        await repo.UpdateStreakAsync(tomorrow);
        await unitOfWork.SaveChangesAsync();

        // Assert
        var streak = await repo.GetUserStreakAsync();
        streak.Should().NotBeNull();
        streak!.CurrentStreak.Should().Be(2);
        streak.LongestStreak.Should().Be(2);
        streak.LastActiveDate.Should().Be(tomorrow);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
