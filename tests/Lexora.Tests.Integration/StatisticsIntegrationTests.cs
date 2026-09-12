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

public class StatisticsIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LexoraDbContext _context;
    private readonly VocabularyRepository _vocabRepo;
    private readonly ReviewRepository _reviewRepo;
    private readonly StatisticsService _statisticsService;

    public StatisticsIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LexoraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LexoraDbContext(options);

        var initializer = new SqliteDatabaseInitializer(_context, NullLogger<SqliteDatabaseInitializer>.Instance);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _vocabRepo = new VocabularyRepository(_context);
        _reviewRepo = new ReviewRepository(_context);
        _statisticsService = new StatisticsService(_vocabRepo, _reviewRepo);
    }

    [Fact]
    public async Task GetDashboardSummary_WithSeededDatabase_ShouldReturnValidMetrics()
    {
        var summary = await _statisticsService.GetDashboardSummaryAsync();

        summary.Should().NotBeNull();
        summary.TotalWords.Should().BeGreaterThan(0);
        summary.AccuracyRate.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GetDetailedAnalytics_WithSeededDatabase_ShouldComputeAllAnalytics()
    {
        // Add a study session with logs to test session and activity retrieval
        var session = new StudySession
        {
            SessionType = "Review_SpacedRepetition",
            TotalCards = 5,
            CorrectCards = 4,
            IncorrectCards = 1,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            CompletedAt = DateTime.UtcNow
        };
        _context.StudySessions.Add(session);
        await _context.SaveChangesAsync();

        var analytics = await _statisticsService.GetDetailedAnalyticsAsync();

        analytics.Should().NotBeNull();
        analytics.Summary.Should().NotBeNull();
        analytics.StatusBreakdown.Should().NotBeEmpty();
        analytics.ExamProgress.Should().NotBeEmpty();
        analytics.CefrDistribution.Should().NotBeEmpty();
        analytics.PosDistribution.Should().NotBeEmpty();
        analytics.Last30DaysHeatmap.Should().HaveCount(30);
        analytics.RetentionCurve.Should().NotBeEmpty();
        analytics.RecentSessions.Should().NotBeEmpty();
        analytics.RecentSessions.First().TotalCards.Should().Be(5);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
