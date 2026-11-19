using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Data.Migrations;

/// <summary>
/// Versioned SQLite schema migration runner using PRAGMA user_version.
/// Ensures non-destructive, idempotent schema upgrades with automatic pre-migration backup.
/// </summary>
public static class VersionedSqliteMigrator
{
    public const int CurrentSchemaVersion = 2;

    public static async Task MigrateToLatestAsync(
        LexoraDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        int currentVersion = await GetUserVersionAsync(connection, cancellationToken);
        if (currentVersion >= CurrentSchemaVersion)
        {
            return;
        }

        logger.LogInformation(
            "SQLite schema version {CurrentVersion} < {TargetVersion}. Running non-destructive migration...",
            currentVersion,
            CurrentSchemaVersion);

        TryCreatePreMigrationBackup(connection, logger);

        if (currentVersion < 2)
        {
            await MigrateToV2Async(connection, logger, cancellationToken);
            await SetUserVersionAsync(connection, 2, cancellationToken);
        }

        logger.LogInformation("SQLite schema migrated successfully to v{TargetVersion}.", CurrentSchemaVersion);
    }

    private static async Task<int> GetUserVersionAsync(DbConnection connection, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    private static async Task SetUserVersionAsync(DbConnection connection, int version, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA user_version = {version};";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void TryCreatePreMigrationBackup(DbConnection connection, ILogger logger)
    {
        try
        {
            if (connection is not SqliteConnection sqliteConn) return;
            var dataSource = sqliteConn.DataSource;
            if (string.IsNullOrWhiteSpace(dataSource) ||
                dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(dataSource))
            {
                return;
            }

            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lexora",
                "backups");
            Directory.CreateDirectory(backupDir);

            var backupPath = Path.Combine(backupDir, $"pre-v{CurrentSchemaVersion}-migration.db");
            if (!File.Exists(backupPath))
            {
                File.Copy(dataSource, backupPath, overwrite: false);
                logger.LogInformation("Created pre-migration database backup at {BackupPath}.", backupPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not create pre-migration backup; continuing with idempotent schema migration.");
        }
    }

    private static async Task MigrateToV2Async(DbConnection connection, ILogger logger, CancellationToken ct)
    {
        var reviewCardColumns = await GetTableColumnsAsync(connection, "ReviewCards", ct);

        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            if (!reviewCardColumns.Contains("DesiredRetention"))
            {
                await ExecuteSqlAsync(connection, transaction, "ALTER TABLE \"ReviewCards\" ADD COLUMN \"DesiredRetention\" REAL NULL;", ct);
            }
            if (!reviewCardColumns.Contains("IsLeech"))
            {
                await ExecuteSqlAsync(connection, transaction, "ALTER TABLE \"ReviewCards\" ADD COLUMN \"IsLeech\" INTEGER NOT NULL DEFAULT 0;", ct);
            }
            if (!reviewCardColumns.Contains("LastElapsedDays"))
            {
                await ExecuteSqlAsync(connection, transaction, "ALTER TABLE \"ReviewCards\" ADD COLUMN \"LastElapsedDays\" REAL NOT NULL DEFAULT 0.0;", ct);
            }
            if (!reviewCardColumns.Contains("AverageResponseTimeMs"))
            {
                await ExecuteSqlAsync(connection, transaction, "ALTER TABLE \"ReviewCards\" ADD COLUMN \"AverageResponseTimeMs\" INTEGER NOT NULL DEFAULT 0;", ct);
            }

            // Backfill IsLeech for existing cards with chronic lapses
            await ExecuteSqlAsync(
                connection,
                transaction,
                "UPDATE \"ReviewCards\" SET \"IsLeech\" = 1 WHERE \"Lapses\" >= 6 OR (\"Lapses\" >= 4 AND \"TotalReviews\" > 0 AND (CAST(\"CorrectReviews\" AS REAL) / \"TotalReviews\") < 0.4);",
                ct);

            await ExecuteSqlAsync(
                connection,
                transaction,
                "CREATE INDEX IF NOT EXISTS \"IX_ReviewCards_IsLeech\" ON \"ReviewCards\" (\"IsLeech\");",
                ct);

            // Create ItemCalibrations table for IRT / Elo Adaptive Quiz
            const string createItemCalibrationsSql = """
                CREATE TABLE IF NOT EXISTS "ItemCalibrations" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_ItemCalibrations" PRIMARY KEY,
                    "WordId" TEXT NULL,
                    "GrammarRuleId" TEXT NULL,
                    "DifficultyB" REAL NOT NULL DEFAULT 0.0,
                    "DiscriminationA" REAL NOT NULL DEFAULT 1.0,
                    "AttemptCount" INTEGER NOT NULL DEFAULT 0,
                    "CorrectCount" INTEGER NOT NULL DEFAULT 0,
                    "MedianResponseTimeMs" INTEGER NOT NULL DEFAULT 0,
                    "LastCalibratedAt" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NULL
                );
                """;
            await ExecuteSqlAsync(connection, transaction, createItemCalibrationsSql, ct);

            await ExecuteSqlAsync(
                connection,
                transaction,
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ItemCalibrations_WordId\" ON \"ItemCalibrations\" (\"WordId\") WHERE \"WordId\" IS NOT NULL;",
                ct);
            await ExecuteSqlAsync(
                connection,
                transaction,
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ItemCalibrations_GrammarRuleId\" ON \"ItemCalibrations\" (\"GrammarRuleId\") WHERE \"GrammarRuleId\" IS NOT NULL;",
                ct);
            await ExecuteSqlAsync(
                connection,
                transaction,
                "CREATE INDEX IF NOT EXISTS \"IX_ItemCalibrations_DifficultyB\" ON \"ItemCalibrations\" (\"DifficultyB\");",
                ct);

            await transaction.CommitAsync(ct);
            logger.LogInformation("Applied v2 schema migration (ReviewCards FSRS-5 telemetry + ItemCalibrations).");
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<HashSet<string>> GetTableColumnsAsync(DbConnection connection, string tableName, CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{tableName}\");";
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            columns.Add(reader.GetString(1));
        }
        return columns;
    }

    private static async Task ExecuteSqlAsync(DbConnection connection, DbTransaction transaction, string sql, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
