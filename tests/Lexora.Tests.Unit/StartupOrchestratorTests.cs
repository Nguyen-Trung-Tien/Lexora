using FluentAssertions;
using Lexora.Infrastructure.Data;
using Lexora.Presentation.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexora.Tests.Unit;

public class StartupOrchestratorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IServiceProvider _serviceProvider;

    public StartupOrchestratorTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<LexoraDbContext>(opt => opt.UseSqlite(_connection));
        services.AddScoped<SqliteDatabaseInitializer>();
        services.AddSingleton<ILocalizationService, LocalizationService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithStartingStateAndDefaultProgress()
    {
        // Arrange & Act
        var orchestrator = new StartupOrchestrator(_serviceProvider, NullLogger<StartupOrchestrator>.Instance);

        // Assert
        orchestrator.CurrentState.Should().Be(StartupState.Starting);
        orchestrator.CurrentProgress.Should().BeApproximately(0.12, 0.01);
        orchestrator.StatusMessage.Should().NotBeNullOrWhiteSpace();
        orchestrator.LastError.Should().BeNull();
    }

    [Fact]
    public async Task InitializeAppAsync_ShouldAdvanceProgressMonotonicallyAndTransitionToReady()
    {
        // Arrange
        var orchestrator = new StartupOrchestrator(_serviceProvider, NullLogger<StartupOrchestrator>.Instance);
        var progressValues = new List<double>();
        var stateValues = new List<StartupState>();

        orchestrator.ProgressChanged += (s, p) => progressValues.Add(p);
        orchestrator.StateChanged += (s, st) => stateValues.Add(st);

        // Act
        await orchestrator.InitializeAppAsync(CancellationToken.None);

        // Assert
        orchestrator.CurrentState.Should().Be(StartupState.Ready);
        orchestrator.CurrentProgress.Should().Be(1.0);
        orchestrator.LastError.Should().BeNull();

        // Check progress monotonicity
        progressValues.Should().NotBeEmpty();
        progressValues.Last().Should().Be(1.0);
        progressValues.Should().BeInAscendingOrder();

        // Check milestone sequence
        progressValues.Should().Contain(p => p >= 0.35 && p <= 0.40); // DB check milestone
        progressValues.Should().Contain(p => p >= 0.70 && p <= 0.75); // Data load milestone

        stateValues.Should().Contain(StartupState.Loading);
        stateValues.Should().Contain(StartupState.Ready);
    }

    [Fact]
    public async Task InitializeAppAsync_WhenCancelled_ShouldTransitionToCancelledState()
    {
        // Arrange
        var orchestrator = new StartupOrchestrator(_serviceProvider, NullLogger<StartupOrchestrator>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        // Act
        await orchestrator.InitializeAppAsync(cts.Token);

        // Assert
        orchestrator.CurrentState.Should().Be(StartupState.Cancelled);
    }

    [Fact]
    public async Task InitializeAppAsync_WhenDatabaseInitializerFails_ShouldTransitionToLoadingError()
    {
        // Arrange: Faulty service provider that fails to resolve database initializer
        var faultyServices = new ServiceCollection();
        faultyServices.AddSingleton<ILocalizationService, LocalizationService>();
        // Notice: SqliteDatabaseInitializer is intentionally not registered
        var faultyProvider = faultyServices.BuildServiceProvider();

        var orchestrator = new StartupOrchestrator(faultyProvider, NullLogger<StartupOrchestrator>.Instance);

        // Act
        await orchestrator.InitializeAppAsync(CancellationToken.None);

        // Assert
        orchestrator.CurrentState.Should().Be(StartupState.LoadingError);
        orchestrator.LastError.Should().NotBeNull();
        orchestrator.StatusMessage.Should().Contain(orchestrator.LastError!.Message);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
