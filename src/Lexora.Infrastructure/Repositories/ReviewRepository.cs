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
            .AsNoTracking()
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
            .AsNoTracking()
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
            .AsNoTracking()
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
            .Include(l => l.ReviewCard)
                .ThenInclude(rc => rc.Word)
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
        var streak = Context.UserStreaks.Local.FirstOrDefault()
            ?? await Context.UserStreaks.FirstOrDefaultAsync(cancellationToken);

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
            else if (date < streak.LastActiveDate.Value)
            {
                // Clock skew, timezone shift, or earlier event — do not overwrite or break streak
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
            .AsNoTracking()
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

    public async Task<IReadOnlyList<ReviewCard>> GetCardsByWordIdsAsync(IReadOnlyList<Guid> wordIds, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(rc => rc.Word)
                .ThenInclude(w => w.Meanings)
                    .ThenInclude(m => m.Examples)
            .Where(rc => wordIds.Contains(rc.WordId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewCard>> GetCardsByTopicIdAsync(Guid topicId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(rc => rc.Word)
                .ThenInclude(w => w.Meanings)
                    .ThenInclude(m => m.Examples)
            .Include(rc => rc.Word)
                .ThenInclude(w => w.WordTopics);

        if (topicId == Guid.Empty)
        {
            return await query
                .Where(rc => !rc.Word.WordTopics.Any())
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        return await query
            .Where(rc => rc.Word.WordTopics.Any(wt => wt.TopicId == topicId))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountDueCardsAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .CountAsync(rc => rc.NextReviewAt <= asOfUtc && rc.Status != LearningStatus.Mastered, cancellationToken);
    }

    public async Task<Dictionary<DateOnly, int>> GetDueForecastCountsAsync(DateTime startUtc, int days, CancellationToken cancellationToken = default)
    {
        var startDate = DateOnly.FromDateTime(startUtc.Date);
        var endUtc = startUtc.Date.AddDays(days);

        var dueDates = await DbSet.AsNoTracking()
            .Where(rc => rc.Status != LearningStatus.Mastered && rc.NextReviewAt <= endUtc)
            .Select(rc => rc.NextReviewAt)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<DateOnly, int>();
        for (int i = 0; i < days; i++)
        {
            result[startDate.AddDays(i)] = 0;
        }

        foreach (var dt in dueDates)
        {
            var cardDate = DateOnly.FromDateTime(dt.Date);
            if (cardDate <= startDate)
            {
                result[startDate]++;
            }
            else if (result.ContainsKey(cardDate))
            {
                result[cardDate]++;
            }
        }

        return result;
    }

    public void AddStudyLog(StudyLog log)
    {
        Context.StudyLogs.Add(log);
    }

    public async Task<StudyLog?> GetLastStudyLogAsync(Guid reviewCardId, CancellationToken cancellationToken = default)
    {
        return await Context.StudyLogs
            .Where(l => l.ReviewCardId == reviewCardId)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task DeleteStudyLogAsync(StudyLog log, CancellationToken cancellationToken = default)
    {
        Context.StudyLogs.Remove(log);
        return Task.CompletedTask;
    }
}
