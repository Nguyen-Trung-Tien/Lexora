using System.Collections.Concurrent;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class AdaptiveQuizService : IAdaptiveQuizService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IUnitOfWork _unitOfWork;

    private class ActiveSessionState
    {
        public Guid SessionId { get; set; }
        public AdaptiveQuizConfigDto Config { get; set; } = null!;
        public CefrLevel CurrentCalibratedLevel { get; set; }
        public int ConsecutiveCorrect { get; set; }
        public int ConsecutiveIncorrect { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public AdaptiveQuestionDto? CurrentQuestion { get; set; }
        public List<AdaptiveAnswerEvaluationDto> Evaluations { get; } = new();
        public List<Guid> UsedWordIds { get; } = new();
        public List<Word> CandidateWordsPool { get; set; } = new();
        public List<Word> DistractorPool { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    private static readonly ConcurrentDictionary<Guid, ActiveSessionState> Sessions = new();

    public AdaptiveQuizService(
        IVocabularyRepository vocabRepo,
        IReviewRepository reviewRepo,
        IUnitOfWork unitOfWork)
    {
        _vocabRepo = vocabRepo;
        _reviewRepo = reviewRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdaptiveQuestionDto?> StartQuizAsync(AdaptiveQuizConfigDto config, CancellationToken cancellationToken = default)
    {
        PruneStaleSessions();

        var sessionId = Guid.NewGuid();
        var allWords = await _vocabRepo.SearchAsync(take: 300, cancellationToken: cancellationToken);
        var distractorPool = allWords.Where(w => w.Meanings.Count > 0).ToList();

        List<Word> candidates;
        if (config.FocusWeakWords)
        {
            var weakCards = await _reviewRepo.GetDifficultCardsAsync(30, cancellationToken);
            var weakWordIds = weakCards.Select(c => c.WordId).ToList();
            var loadedWeak = await _vocabRepo.GetByIdsWithDetailsAsync(weakWordIds, cancellationToken);
            candidates = loadedWeak?.ToList() ?? new List<Word>();

            if (candidates.Count < config.QuestionCount)
            {
                var filler = allWords.Where(w => !candidates.Any(c => c.Id == w.Id)).Take(config.QuestionCount * 2);
                candidates.AddRange(filler);
            }
        }
        else
        {
            candidates = allWords.ToList();
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No vocabulary words available for adaptive quiz.");
        }

        var sessionState = new ActiveSessionState
        {
            SessionId = sessionId,
            Config = config,
            CurrentCalibratedLevel = config.InitialLevel,
            CandidateWordsPool = candidates,
            DistractorPool = distractorPool.Count >= 4 ? distractorPool : candidates,
            CurrentQuestionIndex = 0
        };

        Sessions[sessionId] = sessionState;

        return await GenerateNextQuestionInternalAsync(sessionState, cancellationToken);
    }

    public async Task<AdaptiveAnswerEvaluationDto> SubmitAnswerAsync(
        Guid sessionId,
        int questionIndex,
        int selectedOptionIndex,
        int responseTimeMs,
        CancellationToken cancellationToken = default)
    {
        if (!Sessions.TryGetValue(sessionId, out var session) || session.CurrentQuestion == null)
        {
            throw new InvalidOperationException("Adaptive quiz session not found or expired.");
        }

        var q = session.CurrentQuestion;
        bool isCorrect = selectedOptionIndex == q.CorrectOptionIndex;
        var previousLevel = session.CurrentCalibratedLevel;

        if (isCorrect)
        {
            session.ConsecutiveCorrect++;
            session.ConsecutiveIncorrect = 0;
            // Promote level if 2 consecutive correct answers
            if (session.ConsecutiveCorrect >= 2 && session.CurrentCalibratedLevel < CefrLevel.C2)
            {
                session.CurrentCalibratedLevel = (CefrLevel)((int)session.CurrentCalibratedLevel + 1);
                session.ConsecutiveCorrect = 0;
            }
        }
        else
        {
            session.ConsecutiveIncorrect++;
            session.ConsecutiveCorrect = 0;
            // Demote level if 2 consecutive incorrect answers
            if (session.ConsecutiveIncorrect >= 2 && session.CurrentCalibratedLevel > CefrLevel.A1)
            {
                session.CurrentCalibratedLevel = (CefrLevel)((int)session.CurrentCalibratedLevel - 1);
                session.ConsecutiveIncorrect = 0;
            }
        }

        int totalAnswered = session.Evaluations.Count + 1;
        int correctCount = session.Evaluations.Count(e => e.IsCorrect) + (isCorrect ? 1 : 0);
        double runningScore = Math.Round((double)correctCount / totalAnswered * 100.0, 1);

        string feedback = isCorrect
            ? $"Chính xác! Trình độ thích ứng: {session.CurrentCalibratedLevel}."
            : $"Chưa chính xác. Đáp án đúng: '{q.Options[q.CorrectOptionIndex]}'.";

        var eval = new AdaptiveAnswerEvaluationDto(
            QuestionIndex: questionIndex,
            WordId: q.WordId,
            IsCorrect: isCorrect,
            SelectedOptionIndex: selectedOptionIndex,
            CorrectOptionIndex: q.CorrectOptionIndex,
            ResponseTimeMs: responseTimeMs,
            PreviousCalibratedLevel: previousLevel,
            NewCalibratedLevel: session.CurrentCalibratedLevel,
            RunningScorePercent: runningScore,
            FeedbackMessage: feedback
        );

        session.Evaluations.Add(eval);
        return eval;
    }

    public Task<AdaptiveQuestionDto?> GetNextQuestionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (!Sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult<AdaptiveQuestionDto?>(null);
        }

        if (session.CurrentQuestionIndex >= session.Config.QuestionCount)
        {
            return Task.FromResult<AdaptiveQuestionDto?>(null);
        }

        return GenerateNextQuestionInternalAsync(session, cancellationToken);
    }

    public async Task<AdaptiveQuizSummaryDto> FinishQuizAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (!Sessions.TryRemove(sessionId, out var session))
        {
            throw new InvalidOperationException("Session not found or already finished.");
        }

        int total = session.Evaluations.Count;
        int correct = session.Evaluations.Count(e => e.IsCorrect);
        int incorrect = total - correct;
        double accuracy = total > 0 ? Math.Round((double)correct / total * 100.0, 1) : 0.0;
        int avgResponseTime = total > 0 ? (int)session.Evaluations.Average(e => e.ResponseTimeMs) : 0;

        var weakIds = session.Evaluations.Where(e => !e.IsCorrect).Select(e => e.WordId).Distinct().ToList();
        var weakWordsRaw = await _vocabRepo.GetByIdsWithDetailsAsync(weakIds, cancellationToken);
        var weakWords = weakWordsRaw?.Select(MapToWordDto).ToList() ?? new List<WordDto>();

        var finalLevel = session.CurrentCalibratedLevel;

        var (toeic, ielts, vstep) = finalLevel switch
        {
            CefrLevel.C2 => ("950 - 990", "8.5 - 9.0", "C2 Expert"),
            CefrLevel.C1 => ("850 - 945", "7.0 - 8.0", "C1 Advanced"),
            CefrLevel.B2 => ("700 - 845", "5.5 - 6.5", "B2 Upper-Intermediate"),
            CefrLevel.B1 => ("450 - 695", "4.0 - 5.0", "B1 Intermediate"),
            CefrLevel.A2 => ("250 - 445", "3.0 - 3.5", "A2 Elementary"),
            _ => ("100 - 245", "2.0 - 2.5", "A1 Beginner")
        };

        var strengths = new List<string>();
        var recommendations = new List<string>();

        if (accuracy >= 80.0)
        {
            strengths.Add($"Phản xạ từ vựng xuất sắc ở bậc {finalLevel}.");
            strengths.Add("Độ chính xác cao trong việc phân biệt ngữ nghĩa và từ loại.");
            recommendations.Add("Nâng cao mục tiêu ôn tập sang các đề thi học thuật nâng cao.");
        }
        else if (accuracy >= 50.0)
        {
            strengths.Add($"Nắm vững vốn từ vựng cốt lõi bậc {session.Config.InitialLevel}.");
            recommendations.Add("Tập trung ôn tập lại các từ bị sai vào danh sách Weak Words.");
        }
        else
        {
            recommendations.Add("Khuyến nghị làm bài tập từ vựng kèm ngữ cảnh câu mẫu trước khi kiểm tra lại.");
        }

        // Persist StudySession in database
        var studySession = new StudySession
        {
            StartedAt = session.CreatedAt,
            CompletedAt = DateTime.UtcNow,
            SessionType = "AdaptiveQuiz_CAT",
            TotalCards = total,
            CorrectCards = correct,
            IncorrectCards = incorrect
        };

        await _reviewRepo.CreateStudySessionAsync(studySession, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdaptiveQuizSummaryDto(
            SessionId: sessionId,
            TotalQuestions: total,
            CorrectCount: correct,
            IncorrectCount: incorrect,
            AccuracyPercentage: accuracy,
            AverageResponseTimeMs: avgResponseTime,
            StartingLevel: session.Config.InitialLevel,
            FinalEstimatedLevel: finalLevel,
            EstimatedToeicBand: toeic,
            EstimatedIeltsBand: ielts,
            EstimatedVstepLevel: vstep,
            DifficultWordsToReview: weakWords,
            KeyStrengths: strengths,
            RecommendedNextSteps: recommendations,
            CompletedAt: DateTime.UtcNow
        );
    }

    public bool HasActiveSession(Guid sessionId) => Sessions.ContainsKey(sessionId);

    private Task<AdaptiveQuestionDto?> GenerateNextQuestionInternalAsync(ActiveSessionState session, CancellationToken cancellationToken)
    {
        session.CurrentQuestionIndex++;
        if (session.CurrentQuestionIndex > session.Config.QuestionCount)
        {
            session.CurrentQuestion = null;
            return Task.FromResult<AdaptiveQuestionDto?>(null);
        }

        // Select next word targeting CurrentCalibratedLevel that has not been used yet
        var targetLevel = session.CurrentCalibratedLevel;
        var availableWords = session.CandidateWordsPool
            .Where(w => !session.UsedWordIds.Contains(w.Id))
            .ToList();

        if (availableWords.Count == 0)
        {
            availableWords = session.CandidateWordsPool.ToList();
        }

        var matchedLevelWords = availableWords.Where(w => w.Level == targetLevel).ToList();
        var selectedWord = matchedLevelWords.Count > 0
            ? matchedLevelWords[Random.Shared.Next(matchedLevelWords.Count)]
            : availableWords[Random.Shared.Next(availableWords.Count)];

        session.UsedWordIds.Add(selectedWord.Id);

        // Generate multiple-choice options with realistic distractors
        var meaning = selectedWord.Meanings.FirstOrDefault();
        string correctMeaning = meaning?.DefinitionVi ?? "Đang cập nhật";

        var otherDistractors = session.DistractorPool
            .Where(w => w.Id != selectedWord.Id && w.Meanings.Count > 0)
            .OrderBy(_ => Random.Shared.Next())
            .Take(3)
            .Select(w => w.Meanings.First().DefinitionVi)
            .ToList();

        while (otherDistractors.Count < 3)
        {
            otherDistractors.Add($"Lựa chọn thay thế {otherDistractors.Count + 1}");
        }

        var options = new List<string>(otherDistractors) { correctMeaning };
        // Shuffle options
        options = options.OrderBy(_ => Random.Shared.Next()).ToList();
        int correctIndex = options.IndexOf(correctMeaning);

        var firstExample = meaning?.Examples.FirstOrDefault();
        string? exampleEn = firstExample?.SentenceEn;

        var question = new AdaptiveQuestionDto(
            SessionId: session.SessionId,
            QuestionIndex: session.CurrentQuestionIndex,
            WordId: selectedWord.Id,
            PromptTerm: selectedWord.Term,
            QuestionText: $"Từ '{selectedWord.Term}' ({selectedWord.PrimaryPartOfSpeech}) mang ý nghĩa chính xác nào sau đây?",
            QuestionType: TestType.WordToMeaning,
            DifficultyLevel: selectedWord.Level,
            Options: options,
            CorrectOptionIndex: correctIndex,
            Explanation: $"'{selectedWord.Term}': {correctMeaning} [Cấp độ CEFR: {selectedWord.Level}]",
            ExampleSentence: exampleEn,
            AudioPath: selectedWord.AudioUsPath ?? selectedWord.AudioUkPath
        );

        session.CurrentQuestion = question;
        return Task.FromResult<AdaptiveQuestionDto?>(question);
    }

    private static void PruneStaleSessions()
    {
        var cutoff = DateTime.UtcNow.AddHours(-2);
        foreach (var kvp in Sessions)
        {
            if (kvp.Value.CreatedAt < cutoff)
            {
                Sessions.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static WordDto MapToWordDto(Word w)
    {
        var meanings = w.Meanings.Select(m => new WordMeaningDto(
            m.Id,
            m.PartOfSpeech,
            m.DefinitionVi,
            m.DefinitionEn,
            m.Explanation,
            m.OrderIndex,
            m.Examples.Select(e => new ExampleSentenceDto(e.Id, e.SentenceEn, e.SentenceVi, e.AudioPath)).ToList()
        )).ToList();

        return new WordDto(
            Id: w.Id,
            Term: w.Term,
            PhoneticUk: w.PhoneticUk,
            PhoneticUs: w.PhoneticUs,
            AudioUkPath: w.AudioUkPath,
            AudioUsPath: w.AudioUsPath,
            Level: w.Level,
            PrimaryPartOfSpeech: w.PrimaryPartOfSpeech,
            Notes: w.Notes,
            Meanings: meanings,
            Topics: w.WordTopics.Select(wt => wt.Topic?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList(),
            ExamTags: w.WordExamTags.Select(wet => wet.ExamTag?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList(),
            LearningStatus: w.ReviewCard?.Status,
            NextReviewAt: w.ReviewCard?.NextReviewAt,
            Confidence: w.ReviewCard?.Confidence ?? 0.0,
            IntervalDays: w.ReviewCard?.IntervalDays ?? 0,
            EaseFactor: w.ReviewCard?.EaseFactor ?? 2.5,
            Lapses: w.ReviewCard?.Lapses ?? 0,
            Repetitions: w.ReviewCard?.Repetitions ?? 0,
            TotalReviews: w.ReviewCard?.TotalReviews ?? 0,
            RetentionRate: 100.0
        );
    }
}
