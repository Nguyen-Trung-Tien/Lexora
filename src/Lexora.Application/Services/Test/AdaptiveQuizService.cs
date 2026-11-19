using System.Collections.Concurrent;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Entities.Test;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class AdaptiveQuizService : IAdaptiveQuizService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IItemCalibrationRepository? _calibrationRepo;

    private const double MinTheta = -3.0;
    private const double MaxTheta = 3.0;
    private const double AbilityStepK = 0.55;
    private const double ItemStepK = 0.18;

    private class ActiveSessionState
    {
        public Guid SessionId { get; set; }
        public AdaptiveQuizConfigDto Config { get; set; } = null!;
        public CefrLevel CurrentCalibratedLevel { get; set; }
        public double AbilityTheta { get; set; }
        public double CurrentQuestionDifficultyB { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public AdaptiveQuestionDto? CurrentQuestion { get; set; }
        public List<AdaptiveAnswerEvaluationDto> Evaluations { get; } = new();
        public List<Guid> UsedWordIds { get; } = new();
        public List<Word> CandidateWordsPool { get; set; } = new();
        public List<Word> DistractorPool { get; set; } = new();
        public Dictionary<Guid, ItemCalibration> Calibrations { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    private static readonly ConcurrentDictionary<Guid, ActiveSessionState> Sessions = new();

    public AdaptiveQuizService(
        IVocabularyRepository vocabRepo,
        IReviewRepository reviewRepo,
        IUnitOfWork unitOfWork,
        IItemCalibrationRepository? calibrationRepo = null)
    {
        _vocabRepo = vocabRepo;
        _reviewRepo = reviewRepo;
        _unitOfWork = unitOfWork;
        _calibrationRepo = calibrationRepo;
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

        var calibrations = _calibrationRepo != null
            ? new Dictionary<Guid, ItemCalibration>(await _calibrationRepo.GetByWordIdsAsync(candidates.Select(w => w.Id), cancellationToken))
            : new Dictionary<Guid, ItemCalibration>();

        var sessionState = new ActiveSessionState
        {
            SessionId = sessionId,
            Config = config,
            CurrentCalibratedLevel = config.InitialLevel,
            AbilityTheta = MapCefrToTheta(config.InitialLevel),
            CandidateWordsPool = candidates,
            DistractorPool = distractorPool.Count >= 4 ? distractorPool : candidates,
            Calibrations = calibrations,
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
        if (!Sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException("Adaptive quiz session not found or expired.");
        }

        lock (session)
        {
            var q = session.CurrentQuestion;
            if (q == null)
            {
                throw new InvalidOperationException("Adaptive quiz session not found or expired.");
            }

            if (questionIndex != q.QuestionIndex)
            {
                throw new InvalidOperationException($"Question index mismatch: expected {q.QuestionIndex}, got {questionIndex}.");
            }

            if (session.Evaluations.Any(e => e.QuestionIndex == questionIndex))
            {
                throw new InvalidOperationException($"Question {questionIndex} has already been submitted.");
            }

            bool isCorrect = selectedOptionIndex == q.CorrectOptionIndex;
            var previousLevel = session.CurrentCalibratedLevel;

            // 1PL/Rasch + Elo-Hybrid Ability Update with Response-Time Fluency Weighting
            double b = session.CurrentQuestionDifficultyB;
            double expectedP = 1.0 / (1.0 + Math.Exp(-(session.AbilityTheta - b)));
            double observed = isCorrect ? 1.0 : 0.0;
            double fluencyFactor = ComputeFluencyWeight(isCorrect, responseTimeMs);

            session.AbilityTheta = Math.Clamp(
                Math.Round(session.AbilityTheta + AbilityStepK * fluencyFactor * (observed - expectedP), 3),
                MinTheta,
                MaxTheta);
            session.CurrentCalibratedLevel = MapThetaToCefr(session.AbilityTheta);

            // Update in-memory ItemCalibration for this word
            if (session.Calibrations.TryGetValue(q.WordId, out var cal))
            {
                cal.DifficultyB = Math.Clamp(Math.Round(cal.DifficultyB + ItemStepK * (expectedP - observed), 3), MinTheta, MaxTheta);
                cal.AttemptCount++;
                if (isCorrect) cal.CorrectCount++;
                if (responseTimeMs > 0)
                {
                    cal.MedianResponseTimeMs = cal.MedianResponseTimeMs <= 0
                        ? responseTimeMs
                        : (int)Math.Round(cal.MedianResponseTimeMs * 0.7 + responseTimeMs * 0.3);
                }
                cal.LastCalibratedAt = DateTime.UtcNow;
                cal.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var word = session.CandidateWordsPool.FirstOrDefault(w => w.Id == q.WordId);
                double baseB = word != null ? MapCefrToTheta(word.Level) : b;
                session.Calibrations[q.WordId] = new ItemCalibration
                {
                    WordId = q.WordId,
                    DifficultyB = Math.Clamp(Math.Round(baseB + ItemStepK * (expectedP - observed), 3), MinTheta, MaxTheta),
                    DiscriminationA = 1.0,
                    AttemptCount = 1,
                    CorrectCount = isCorrect ? 1 : 0,
                    MedianResponseTimeMs = Math.Max(0, responseTimeMs),
                    LastCalibratedAt = DateTime.UtcNow
                };
            }

            int totalAnswered = session.Evaluations.Count + 1;
            int correctCount = session.Evaluations.Count(e => e.IsCorrect) + (isCorrect ? 1 : 0);
            double runningScore = Math.Round((double)correctCount / totalAnswered * 100.0, 1);

            string feedback = isCorrect
                ? $"Chính xác! Trình độ thích ứng: {session.CurrentCalibratedLevel} (θ = {session.AbilityTheta:F2})."
                : $"Chưa chính xác. Đáp án đúng: '{q.Options[q.CorrectOptionIndex]}' (θ = {session.AbilityTheta:F2}).";

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
                FeedbackMessage: feedback,
                EstimatedAbilityTheta: session.AbilityTheta
            );

            session.Evaluations.Add(eval);
            return eval;
        }
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

        // Persist updated IRT ItemCalibrations atomically
        if (_calibrationRepo != null && session.Evaluations.Count > 0)
        {
            var answeredWordIds = session.Evaluations.Select(e => e.WordId).Distinct().ToList();
            var existingInDb = await _calibrationRepo.GetByWordIdsAsync(answeredWordIds, cancellationToken);
            foreach (var wordId in answeredWordIds)
            {
                if (!session.Calibrations.TryGetValue(wordId, out var cal)) continue;
                if (existingInDb.ContainsKey(wordId))
                {
                    await _calibrationRepo.UpdateAsync(cal, cancellationToken);
                }
                else
                {
                    await _calibrationRepo.AddAsync(cal, cancellationToken);
                }
            }
        }

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
            CompletedAt: DateTime.UtcNow,
            FinalAbilityTheta: session.AbilityTheta
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

        // Maximum Fisher Information selection: choose unused word with difficulty b closest to current theta
        var availableWords = session.CandidateWordsPool
            .Where(w => !session.UsedWordIds.Contains(w.Id))
            .ToList();

        if (availableWords.Count == 0)
        {
            availableWords = session.CandidateWordsPool.ToList();
        }

        var rankedCandidates = availableWords
            .Select(w => (Word: w, DiffB: GetWordDifficultyB(session, w)))
            .Select(x => (x.Word, x.DiffB, Dist: Math.Abs(x.DiffB - session.AbilityTheta)))
            .OrderBy(x => x.Dist)
            .ToList();

        double minDist = rankedCandidates[0].Dist;
        var bestTier = rankedCandidates
            .Where(x => Math.Abs(x.Dist - minDist) <= 0.25)
            .Take(5)
            .ToList();

        var chosen = bestTier[Random.Shared.Next(bestTier.Count)];
        var selectedWord = chosen.Word;
        session.CurrentQuestionDifficultyB = chosen.DiffB;
        session.UsedWordIds.Add(selectedWord.Id);

        // Smart Distractor Selection: prioritize same PartOfSpeech or shared Topic
        var meaning = selectedWord.Meanings.FirstOrDefault();
        string correctMeaning = meaning?.DefinitionVi ?? "Đang cập nhật";
        var selectedTopicIds = selectedWord.WordTopics.Select(wt => wt.TopicId).ToHashSet();

        var otherDistractors = session.DistractorPool
            .Where(w => w.Id != selectedWord.Id && w.Meanings.Count > 0)
            .Where(w => !string.Equals(w.Meanings.First().DefinitionVi, correctMeaning, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(w =>
                (w.PrimaryPartOfSpeech == selectedWord.PrimaryPartOfSpeech ? 2 : 0) +
                (w.WordTopics.Any(wt => selectedTopicIds.Contains(wt.TopicId)) ? 2 : 0) +
                (Math.Abs((int)w.Level - (int)selectedWord.Level) <= 1 ? 1 : 0))
            .ThenBy(_ => Random.Shared.Next())
            .Take(3)
            .Select(w => w.Meanings.First().DefinitionVi)
            .Distinct(StringComparer.OrdinalIgnoreCase)
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
            Explanation: $"'{selectedWord.Term}': {correctMeaning} [CEFR: {selectedWord.Level} | b={chosen.DiffB:F2}]",
            ExampleSentence: exampleEn,
            AudioPath: selectedWord.AudioUsPath ?? selectedWord.AudioUkPath,
            ItemDifficultyB: chosen.DiffB,
            CurrentTheta: session.AbilityTheta
        );

        session.CurrentQuestion = question;
        return Task.FromResult<AdaptiveQuestionDto?>(question);
    }

    private static double GetWordDifficultyB(ActiveSessionState session, Word word)
    {
        if (session.Calibrations.TryGetValue(word.Id, out var cal) && cal.AttemptCount > 0)
        {
            return cal.DifficultyB;
        }
        return MapCefrToTheta(word.Level);
    }

    private static double ComputeFluencyWeight(bool isCorrect, int responseTimeMs)
    {
        if (responseTimeMs <= 0) return 1.0;
        if (isCorrect)
        {
            if (responseTimeMs <= 3500) return 1.20;
            if (responseTimeMs >= 10000) return 0.65;
            return 1.0;
        }
        return responseTimeMs <= 2500 ? 1.10 : 1.0;
    }

    private static double MapCefrToTheta(CefrLevel level) => level switch
    {
        CefrLevel.A1 => -2.0,
        CefrLevel.A2 => -1.0,
        CefrLevel.B1 => 0.0,
        CefrLevel.B2 => 1.0,
        CefrLevel.C1 => 2.0,
        CefrLevel.C2 => 2.8,
        _ => 0.0
    };

    private static CefrLevel MapThetaToCefr(double theta) => theta switch
    {
        < -1.5 => CefrLevel.A1,
        < -0.5 => CefrLevel.A2,
        < 0.5 => CefrLevel.B1,
        < 1.5 => CefrLevel.B2,
        < 2.4 => CefrLevel.C1,
        _ => CefrLevel.C2
    };

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
