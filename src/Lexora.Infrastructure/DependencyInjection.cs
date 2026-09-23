using Lexora.Application.Interfaces;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Lexora.Infrastructure.Repositories;
using Lexora.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? databasePath = null)
    {
        var dbPath = databasePath;
        if (string.IsNullOrEmpty(dbPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lexoraDir = Path.Combine(appData, "Lexora", "data");
            Directory.CreateDirectory(lexoraDir);
            dbPath = Path.Combine(lexoraDir, "lexora.db");
        }

        var connectionString = $"Data Source={dbPath};Default Timeout=15;Cache=Shared;Foreign Keys=True;";

        services.AddDbContext<LexoraDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        // Repositories & Unit of Work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IVocabularyRepository, VocabularyRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IGrammarRepository, GrammarRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Initializer
        services.AddScoped<SqliteDatabaseInitializer>();

        // File & Backup services
        services.AddSingleton<IAudioStorageService, LocalAudioStorageService>();
        services.AddSingleton<IDatabaseBackupService>(sp =>
            new DatabaseBackupService(dbPath, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseBackupService>>()));

        // Review reminder & notifications
        services.AddScoped<IReviewReminderService, ReviewReminderService>();

        // User preferences persistence
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();

        return services;
    }
}
