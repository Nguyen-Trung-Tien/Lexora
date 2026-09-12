using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

public class ReviewRepository : Repository<ReviewCard>, IReviewRepository
{
    public ReviewRepository(LexoraDbContext context) : base(context)
    {
    }

    public async Task<ReviewCard?> GetByWordIdAsync(Guid wordId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(rc => rc.Word)
                .ThenInclude(w => w.Meanings)
                    .ThenInclude(m => m.Examples)
            .FirstOrDefaultAsync(rc => rc.WordId == wordId, cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewCard>> GetDueCardsAsync(DateTime asOfUtc, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(rc => rc.Word)
                .ThenInclude(w => w.Meanings)
                    .ThenInclude(m => m.Examples)
            .Where(rc => rc.NextReviewAt <= asOfUtc && rc.Status != LearningStatus.Mastered)
            .OrderBy(rc => rc.NextReviewAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewCard>> GetNewCardsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(rc => rc.Word)
                .ThenInclude(w => w.Meanings)
                    .ThenInclude(m => m.Examples)
            .Where(rc => rc.Status == LearningStatus.New)
            .OrderBy(rc => rc.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewCard>> GetCardsByStatusAsync(LearningStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(rc => rc.Word)
            .Where(rc => rc.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<LearningStatus, int>> GetCardCountsByStatusAsync(CancellationToken cancellationToken = default)
    {
        var grouped = await DbSet
            .GroupBy(rc => rc.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped.ToDictionary(g => g.Status, g => g.Count);
    }

    public async Task<IReadOnlyList<StudyLog>> GetRecentLogsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await Context.StudyLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserStreak?> GetUserStreakAsync(CancellationToken cancellationToken = default)
    {
        return await Context.UserStreaks.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateStreakAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var streak = await Context.UserStreaks.FirstOrDefaultAsync(cancellationToken);
        if (streak == null)
        {
            streak = new UserStreak
            {
                CurrentStreak = 1,
                LongestStreak = 1,
                LastActiveDate = date,
                TotalDaysLearned = 1
            };
            await Context.UserStreaks.AddAsync(streak, cancellationToken);
        }
        else
        {
            if (streak.LastActiveDate == null)
            {
                streak.CurrentStreak = 1;
                streak.LongestStreak = Math.Max(streak.LongestStreak, 1);
                streak.LastActiveDate = date;
                streak.TotalDaysLearned = 1;
            }
            else if (streak.LastActiveDate == date)
            {
                // Already studied today, streak stays the same
            }
            else if (streak.LastActiveDate == date.AddDays(-1))
            {
                // Consecutive day!
                streak.CurrentStreak++;
                streak.LongestStreak = Math.Max(streak.LongestStreak, streak.CurrentStreak);
                streak.LastActiveDate = date;
                streak.TotalDaysLearned++;
            }
            else
            {
                // Streak broken
                streak.CurrentStreak = 1;
                streak.LastActiveDate = date;
                streak.TotalDaysLearned++;
            }
        }
    }

    public async Task<IReadOnlyList<ReviewCard>> GetDifficultCardsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(rc => rc.Word)
                .ThenInclude(w => w.Meanings)
                    .ThenInclude(m => m.Examples)
            .Where(rc => rc.Lapses > 0 || rc.Status == LearningStatus.NotRemembered)
            .OrderByDescending(rc => rc.Lapses)
            .ThenBy(rc => rc.NextReviewAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<StudySession> CreateStudySessionAsync(StudySession session, CancellationToken cancellationToken = default)
    {
        await Context.StudySessions.AddAsync(session, cancellationToken);
        return session;
    }

    public async Task<StudySession?> GetStudySessionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.StudySessions
            .Include(ss => ss.StudyLogs)
            .FirstOrDefaultAsync(ss => ss.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StudySession>> GetRecentSessionsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await Context.StudySessions
            .AsNoTracking()
            .OrderByDescending(ss => ss.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateStudySessionAsync(StudySession session, CancellationToken cancellationToken = default)
    {
        var entry = Context.Entry(session);
        if (entry.State == EntityState.Detached)
        {
            Context.StudySessions.Update(session);
        }
        return Task.CompletedTask;
    }

    public void AddStudyLog(StudyLog log)
    {
        Context.StudyLogs.Add(log);
    }
}
