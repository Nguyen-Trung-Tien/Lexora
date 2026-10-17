using System.Data;
using System.Diagnostics;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Services;

public class DataHealthService : IDataHealthService
{
    private readonly LexoraDbContext _context;
    private readonly IAudioStorageService _audioStorage;
    private readonly ILogger<DataHealthService> _logger;
    private readonly string _databasePath;

    public DataHealthService(
        LexoraDbContext context,
        IAudioStorageService audioStorage,
        IDatabaseBackupService backupService,
        ILogger<DataHealthService> logger)
    {
        _context = context;
        _audioStorage = audioStorage;
        _logger = logger;
        _databasePath = backupService.DatabasePath;
    }

    public async Task<DatabaseHealthReportDto> RunDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running comprehensive SQLite database diagnostics...");

        string integrityResult = "OK";
        string quickCheckResult = "OK";
        int pageCount = 0;
        int pageSize = 4096;
        int freelistCount = 0;
        long dbSizeBytes = 0;
        long walSizeBytes = 0;

        // 1. File size telemetry
        if (File.Exists(_databasePath))
        {
            var dbFileInfo = new FileInfo(_databasePath);
            dbSizeBytes = dbFileInfo.Length;

            var walPath = _databasePath + "-wal";
            if (File.Exists(walPath))
            {
                walSizeBytes = new FileInfo(walPath).Length;
            }
        }

        // 2. Low-level PRAGMA checks using ADO.NET connection
        var connection = (SqliteConnection)_context.Database.GetDbConnection();
        bool wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            // Integrity Check
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA integrity_check(5);";
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var messages = new List<string>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    messages.Add(reader.GetString(0));
                }
                integrityResult = messages.Count > 0 ? string.Join("; ", messages) : "OK";
            }

            // Quick Check
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA quick_check(5);";
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var messages = new List<string>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    messages.Add(reader.GetString(0));
                }
                quickCheckResult = messages.Count > 0 ? string.Join("; ", messages) : "OK";
            }

            // Page stats
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA page_count;";
                var res = await cmd.ExecuteScalarAsync(cancellationToken);
                pageCount = Convert.ToInt32(res);
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA page_size;";
                var res = await cmd.ExecuteScalarAsync(cancellationToken);
                pageSize = Convert.ToInt32(res);
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA freelist_count;";
                var res = await cmd.ExecuteScalarAsync(cancellationToken);
                freelistCount = Convert.ToInt32(res);
            }
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }

        double fragmentationPercent = pageCount > 0
            ? Math.Round((double)freelistCount / pageCount * 100.0, 1)
            : 0.0;

        // 3. Domain & Data Consistency Auditing
        int orphanMeanings = await _context.WordMeanings
            .Where(m => !_context.Words.Any(w => w.Id == m.WordId))
            .CountAsync(cancellationToken);

        int orphanExamples = await _context.ExampleSentences
            .Where(e => !_context.WordMeanings.Any(m => m.Id == e.WordMeaningId))
            .CountAsync(cancellationToken);

        int orphanStudyLogs = await _context.StudyLogs
            .Where(l => !_context.ReviewCards.Any(c => c.Id == l.ReviewCardId))
            .CountAsync(cancellationToken);

        int missingReviewCards = await _context.Words
            .Where(w => !_context.ReviewCards.Any(c => c.WordId == w.Id))
            .CountAsync(cancellationToken);

        int invalidReviewCards = await _context.ReviewCards
            .Where(c => c.IntervalDays < 1 || c.EaseFactor < 1.3)
            .CountAsync(cancellationToken);

        var terms = await _context.Words
            .Select(w => w.NormalizedTerm)
            .ToListAsync(cancellationToken);

        int duplicateTerms = terms.GroupBy(t => t).Count(g => g.Count() > 1);

        // 4. Audio Cache Auditing
        int audioFilesCount = 0;
        long audioSizeBytes = 0;
        var audioDir = _audioStorage.GetAudioCacheDirectory();
        if (Directory.Exists(audioDir))
        {
            var files = Directory.GetFiles(audioDir, "*.*", SearchOption.TopDirectoryOnly);
            audioFilesCount = files.Length;
            foreach (var f in files)
            {
                try { audioSizeBytes += new FileInfo(f).Length; } catch { }
            }
        }

        // 5. Compute Health Score (0 - 100)
        int healthScore = 100;
        if (!string.Equals(integrityResult, "ok", StringComparison.OrdinalIgnoreCase)) healthScore -= 50;
        if (!string.Equals(quickCheckResult, "ok", StringComparison.OrdinalIgnoreCase)) healthScore -= 30;
        if (orphanMeanings > 0) healthScore -= Math.Min(15, orphanMeanings * 2);
        if (orphanExamples > 0) healthScore -= Math.Min(10, orphanExamples * 2);
        if (missingReviewCards > 0) healthScore -= Math.Min(20, missingReviewCards * 3);
        if (invalidReviewCards > 0) healthScore -= Math.Min(15, invalidReviewCards * 2);
        if (fragmentationPercent > 30.0) healthScore -= 10;
        if (duplicateTerms > 0) healthScore -= Math.Min(10, duplicateTerms * 2);

        healthScore = Math.Clamp(healthScore, 0, 100);

        var rating = healthScore switch
        {
            >= 90 => DatabaseHealthRating.Optimal,
            >= 75 => DatabaseHealthRating.Good,
            >= 50 => DatabaseHealthRating.NeedsMaintenance,
            _ => DatabaseHealthRating.Critical
        };

        return new DatabaseHealthReportDto(
            SqliteIntegrityStatus: integrityResult,
            QuickCheckStatus: quickCheckResult,
            DatabaseSizeBytes: dbSizeBytes,
            PageCount: pageCount,
            PageSize: pageSize,
            FreelistCount: freelistCount,
            FragmentationPercent: fragmentationPercent,
            WalFileSizeBytes: walSizeBytes,
            OrphanMeaningsCount: orphanMeanings,
            OrphanExamplesCount: orphanExamples,
            OrphanStudyLogsCount: orphanStudyLogs,
            InvalidReviewCardsCount: invalidReviewCards,
            MissingReviewCardsCount: missingReviewCards,
            DuplicateTermsCount: duplicateTerms,
            AudioCacheFilesCount: audioFilesCount,
            AudioCacheSizeBytes: audioSizeBytes,
            HealthScore: healthScore,
            HealthRating: rating,
            CheckedAt: DateTime.UtcNow
        );
    }

    public async Task<DataRepairResultDto> RepairAndOptimizeAsync(
        bool vacuumDatabase = true,
        bool clearAudioCache = false,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Starting database repair and optimization...");

        long initialSizeBytes = File.Exists(_databasePath) ? new FileInfo(_databasePath).Length : 0;
        int repairedOrphans = 0;
        int reconciledCards = 0;
        int fixedIntervals = 0;

        // Step 1: Fix invalid review cards (Interval < 1 or EaseFactor < 1.3)
        var invalidCards = await _context.ReviewCards
            .Where(c => c.IntervalDays < 1 || c.EaseFactor < 1.3)
            .ToListAsync(cancellationToken);

        foreach (var card in invalidCards)
        {
            if (card.IntervalDays < 1) card.IntervalDays = 1;
            if (card.EaseFactor < 1.3) card.EaseFactor = 1.3;
            fixedIntervals++;
        }

        // Step 2: Reconcile missing ReviewCards for existing words
        var wordsWithoutCard = await _context.Words
            .Where(w => !_context.ReviewCards.Any(c => c.WordId == w.Id))
            .ToListAsync(cancellationToken);

        foreach (var word in wordsWithoutCard)
        {
            var newCard = new ReviewCard
            {
                Id = Guid.NewGuid(),
                WordId = word.Id,
                Status = LearningStatus.NotRemembered,
                IntervalDays = 1,
                EaseFactor = 2.5,
                Repetitions = 0,
                Lapses = 0,
                NextReviewAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ReviewCards.Add(newCard);
            reconciledCards++;
        }

        // Step 3: Remove orphaned records
        var orphanMeanings = await _context.WordMeanings
            .Where(m => !_context.Words.Any(w => w.Id == m.WordId))
            .ToListAsync(cancellationToken);
        if (orphanMeanings.Count > 0)
        {
            _context.WordMeanings.RemoveRange(orphanMeanings);
            repairedOrphans += orphanMeanings.Count;
        }

        var orphanExamples = await _context.ExampleSentences
            .Where(e => !_context.WordMeanings.Any(m => m.Id == e.WordMeaningId))
            .ToListAsync(cancellationToken);
        if (orphanExamples.Count > 0)
        {
            _context.ExampleSentences.RemoveRange(orphanExamples);
            repairedOrphans += orphanExamples.Count;
        }

        var orphanLogs = await _context.StudyLogs
            .Where(l => !_context.ReviewCards.Any(c => c.Id == l.ReviewCardId))
            .ToListAsync(cancellationToken);
        if (orphanLogs.Count > 0)
        {
            _context.StudyLogs.RemoveRange(orphanLogs);
            repairedOrphans += orphanLogs.Count;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Step 4: Optional Audio Cache purge
        if (clearAudioCache)
        {
            await ClearAudioCacheAsync(cancellationToken);
        }

        // Step 5: VACUUM and SQLite Optimization
        if (vacuumDatabase)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("PRAGMA optimize;", cancellationToken);
                await _context.Database.ExecuteSqlRawAsync("VACUUM;", cancellationToken);
                await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "VACUUM optimization encountered non-critical exception: {Message}", ex.Message);
            }
        }

        sw.Stop();
        long finalSizeBytes = File.Exists(_databasePath) ? new FileInfo(_databasePath).Length : 0;
        long bytesReclaimed = Math.Max(0, initialSizeBytes - finalSizeBytes);

        string summary = $"Repaired {repairedOrphans} orphaned items, reconciled {reconciledCards} missing cards, fixed {fixedIntervals} cards. Reclaimed {bytesReclaimed / 1024.0:F1} KB in {sw.ElapsedMilliseconds}ms.";

        return new DataRepairResultDto(
            RepairedOrphansCount: repairedOrphans,
            ReconciledCardsCount: reconciledCards,
            FixedIntervalsCount: fixedIntervals,
            BytesReclaimed: bytesReclaimed,
            OptimizationDurationMs: sw.ElapsedMilliseconds,
            Success: true,
            Summary: summary,
            CompletedAt: DateTime.UtcNow
        );
    }

    public Task<long> ClearAudioCacheAsync(CancellationToken cancellationToken = default)
    {
        long freedBytes = 0;
        var audioDir = _audioStorage.GetAudioCacheDirectory();
        if (Directory.Exists(audioDir))
        {
            var files = Directory.GetFiles(audioDir, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var f in files)
            {
                try
                {
                    var len = new FileInfo(f).Length;
                    File.Delete(f);
                    freedBytes += len;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete cached audio file {File}", f);
                }
            }
        }

        return Task.FromResult(freedBytes);
    }
}
