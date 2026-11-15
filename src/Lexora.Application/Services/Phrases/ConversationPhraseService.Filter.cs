using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Domain.Entities.Phrases;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services.Phrases;

public partial class ConversationPhraseService
{
    private static readonly string[] DefaultScenarios =
    [
        "Workplace & Office",
        "Business & TOEIC",
        "Academic & IELTS Speaking",
        "Travel & Airport",
        "Daily Life & Social",
        "VSTEP Practical Situations",
        "Customer Service",
        "Meetings & Presentations"
    ];

    public async Task<IReadOnlyList<ConversationPhraseDto>> GetPhrasesAsync(
        ConversationPhraseFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        var phrases = await FetchFilteredEntitiesAsync(filter, filter?.SearchQuery, cancellationToken);

        // Fuzzy search fallback when exact/SQL LIKE search returns zero rows
        if (phrases.Count == 0 && !string.IsNullOrWhiteSpace(filter?.SearchQuery))
        {
            var candidates = await FetchFilteredEntitiesAsync(filter, searchQuery: null, cancellationToken);
            phrases = RankWithFuzzyFallback(filter.SearchQuery.Trim(), candidates);
        }

        return phrases.Select(p => p.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var dbCategories = await repository.GetCategoriesAsync(cancellationToken);
        return dbCategories
            .Concat(DefaultScenarios)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<IReadOnlyList<PhraseSmartSuggestionDto>> GetQuickTemplatesAsync(
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var templates = BuildBuiltInTemplates();
        if (!string.IsNullOrWhiteSpace(category))
        {
            var trimmed = category.Trim();
            var prioritized = templates
                .OrderByDescending(t => t.CategoryTag.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult<IReadOnlyList<PhraseSmartSuggestionDto>>(prioritized);
        }

        return Task.FromResult<IReadOnlyList<PhraseSmartSuggestionDto>>(templates);
    }

    public Task<string> GenerateEnhancedSuggestionsAsync(
        string promptSentence,
        string sampleResponse,
        string category,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(promptSentence) && string.IsNullOrWhiteSpace(sampleResponse))
        {
            return Task.FromResult(string.Empty);
        }

        var scenario = string.IsNullOrWhiteSpace(category) ? "Daily Life" : category.Trim();
        var suggestion = string.Join(Environment.NewLine,
        [
            $"Ngữ cảnh áp dụng: {scenario}",
            $"• Formal variation: Could we align on next steps for \"{sampleResponse.Trim()}\" at your earliest convenience?",
            $"• Casual variation: Sounds good — I'll jump on that right away.",
            $"• Vietnamese Learner Tip: Avoid word-for-word translation from Vietnamese; keep subject-verb agreement and natural linking phrases clear."
        ]);

        return Task.FromResult(suggestion);
    }

    private Task<IReadOnlyList<ConversationPhrase>> FetchFilteredEntitiesAsync(
        ConversationPhraseFilterDto? filter,
        string? searchQuery,
        CancellationToken cancellationToken)
    {
        var hasAdvancedFilters = filter is not null &&
            (filter.Register.HasValue || filter.ExamTarget.HasValue || filter.OnlyDueForReview.HasValue);

        if (!hasAdvancedFilters)
        {
            return repository.GetFilteredAsync(
                filter?.Category,
                filter?.CefrLevel,
                filter?.OnlyFavorites,
                searchQuery,
                cancellationToken);
        }

        return repository.GetFilteredAdvancedAsync(
            filter!.Category,
            filter.CefrLevel,
            filter.Register,
            filter.ExamTarget,
            filter.OnlyFavorites,
            filter.OnlyDueForReview,
            searchQuery,
            cancellationToken);
    }

    private IReadOnlyList<ConversationPhrase> RankWithFuzzyFallback(
        string query,
        IReadOnlyList<ConversationPhrase> candidates)
    {
        if (candidates.Count == 0 || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();
        var queryTokens = normalizedQuery.Split([' ', ',', '.', '?', '!'], StringSplitOptions.RemoveEmptyEntries);
        int maxTokenDist = Math.Clamp(normalizedQuery.Length / 4 + 1, 1, 3);

        var scored = new List<(ConversationPhrase Phrase, int Score)>();
        foreach (var phrase in candidates)
        {
            int best = ComputeDistance(normalizedQuery, phrase.NormalizedPrompt);
            if (!string.IsNullOrWhiteSpace(phrase.ContextTag))
            {
                best = Math.Min(best, ComputeDistance(normalizedQuery, phrase.ContextTag.Trim().ToLowerInvariant()));
            }

            foreach (var qTok in queryTokens)
            {
                int tokLimit = Math.Clamp(qTok.Length / 4 + 1, 1, 3);
                int tokScore = ComputeTokenBestDistance(qTok, phrase);
                if (tokScore <= tokLimit)
                {
                    best = Math.Min(best, tokScore);
                }
            }

            if (best <= maxTokenDist)
            {
                scored.Add((phrase, best));
            }
        }

        return scored
            .OrderBy(x => x.Score)
            .ThenByDescending(x => x.Phrase.IsFavorite)
            .Select(x => x.Phrase)
            .Take(30)
            .ToList();
    }

    private int ComputeDistance(string a, string b)
        => fuzzySearchService is not null
            ? fuzzySearchService.CalculateDistance(a, b)
            : ComputeLevenshteinDistance(a, b);

    private int ComputeTokenBestDistance(string query, ConversationPhrase phrase)
    {
        var tokens = $"{phrase.PromptSentence} {phrase.SampleResponse} {phrase.KeyPhrases} {phrase.ContextTag} {phrase.Category}"
            .Split([' ', ',', '.', '?', '!', ';', ':', '-', '/', '(', ')'], StringSplitOptions.RemoveEmptyEntries);

        var best = int.MaxValue;
        foreach (var token in tokens)
        {
            var dist = ComputeDistance(query, token.ToLowerInvariant());
            if (dist < best) best = dist;
        }
        return best;
    }

    private static int ComputeLevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b.Length;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) prev[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }

        return prev[b.Length];
    }

    private static List<PhraseSmartSuggestionDto> BuildBuiltInTemplates() =>
    [
        new()
        {
            Title = "Deadline Follow-up (TOEIC/Workplace)",
            Description = "Hỏi tiến độ lịch sự trong email hoặc họp nhóm.",
            TextToAppend = "Could you share a quick status update on this deliverable before EOD?",
            CategoryTag = "Formal",
            Register = PhraseRegister.Formal,
            ExamTarget = ExamTarget.Toeic,
            KeyPhrases = "share a quick status update; deliverable; before EOD",
            FormalVariation = "Would it be possible to provide an update on the current timeline by end of day?",
            CasualVariation = "Hey, any quick update on where we stand with this?",
            CommonMistakesVi = "Tránh nói cộc lốc 'Give me update' hoặc quên cụm lịch sự 'Could you...'."
        },
        new()
        {
            Title = "Buying Time & Structuring (IELTS Speaking)",
            Description = "Mở đầu câu trả lời mạch lạc trong IELTS Part 2 & Part 3.",
            TextToAppend = "That's a compelling question. From my perspective, the main factor comes down to...",
            CategoryTag = "Academic & IELTS Speaking",
            Register = PhraseRegister.Formal,
            ExamTarget = ExamTarget.Ielts,
            KeyPhrases = "From my perspective; comes down to; compelling question",
            FormalVariation = "Looking at the broader picture, one decisive factor is...",
            CasualVariation = "Good question — honestly, I think it mostly comes down to...",
            CommonMistakesVi = "Tránh lặp quá nhiều 'Actually I think...' hay ậm ừ 'ờm...' quá lâu."
        },
        new()
        {
            Title = "Airport Gate & Rebooking (Travel)",
            Description = "Xử lý đổi chuyến bay hoặc hỏi thông tin cổng ra máy bay.",
            TextToAppend = "Excuse me, my connecting flight was delayed. Could you help me rebook the next available one?",
            CategoryTag = "Travel & Airport",
            Register = PhraseRegister.Neutral,
            ExamTarget = ExamTarget.Toeic,
            KeyPhrases = "connecting flight; delayed; next available",
            FormalVariation = "Pardon me, due to an earlier delay, could you assist with rebooking my itinerary?",
            CasualVariation = "Hi, I missed my connection — can we get me onto the next flight?",
            CommonMistakesVi = "Nhớ dùng 'next available flight' thay vì 'nearest next flight'."
        },
        new()
        {
            Title = "Clarifying & Confirming Decision (VSTEP)",
            Description = "Xác nhận lựa chọn tối ưu trong bài thảo luận VSTEP Part 2.",
            TextToAppend = "Just to make sure we're on the same page, option B seems like the most practical choice overall.",
            CategoryTag = "VSTEP Practical Situations",
            Register = PhraseRegister.Neutral,
            ExamTarget = ExamTarget.Vstep,
            KeyPhrases = "on the same page; practical choice; overall",
            FormalVariation = "To ensure alignment, option B appears to be the most viable alternative.",
            CasualVariation = "So just to be sure we agree, let's go with option B.",
            CommonMistakesVi = "Tránh dịch word-by-word 'I choose B because B is best' thiếu lý do."
        },
        new()
        {
            Title = "Warm Social Check-in (Daily Life)",
            Description = "Bắt chuyện tự nhiên với bạn bè hoặc đồng nghiệp thân thiết.",
            TextToAppend = "How have things been on your end lately? Long time no see!",
            CategoryTag = "Casual",
            Register = PhraseRegister.Casual,
            KeyPhrases = "on your end; lately; Long time no see",
            FormalVariation = "It's a pleasure catching up with you again. How have you been?",
            CasualVariation = "Hey! What's been keeping you busy lately?",
            CommonMistakesVi = "Tránh hỏi lặp lại 'How are you? I'm fine, thank you, and you?' một cách máy móc."
        }
    ];
}
