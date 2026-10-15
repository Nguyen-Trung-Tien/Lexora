using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;
using Lexora.Domain.Interfaces;

namespace Lexora.Application.Services;

public class TestService : ITestService
{
    private readonly IVocabularyRepository _vocabRepo;
    private readonly IReviewRepository _reviewRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILearningStorageRepository? _storageRepo;

    private record ActiveSessionEntry(DateTime CreatedAt, List<TestQuestionDto> Questions);
    private static readonly ConcurrentDictionary<Guid, ActiveSessionEntry> ActiveTestSessions = new();

    public TestService(
        IVocabularyRepository vocabRepo,
        IReviewRepository reviewRepo,
        IUnitOfWork unitOfWork,
        ILearningStorageRepository? storageRepo = null)
    {
        _vocabRepo = vocabRepo;
        _reviewRepo = reviewRepo;
        _unitOfWork = unitOfWork;
        _storageRepo = storageRepo;
    }

    public async Task<TestSessionDto> GenerateTestAsync(TestConfigurationDto config, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Word> candidateWords;

        if (config.CustomWordIds != null && config.CustomWordIds.Count > 0)
        {
            candidateWords = await _vocabRepo.GetByIdsWithDetailsAsync(config.CustomWordIds, cancellationToken)
                             ?? Array.Empty<Word>();
        }
        else if (config.WeakWordsOnly)
        {
            var difficultCards = await _reviewRepo.GetDifficultCardsAsync(50, cancellationToken);
            var weakWordIds = difficultCards.Select(c => c.WordId).ToList();
            var loadedWords = (await _vocabRepo.GetByIdsWithDetailsAsync(weakWordIds, cancellationToken)
                               ?? Array.Empty<Word>()).ToList();

            candidateWords = loadedWords;

            // If not enough weak words, backfill with general vocabulary
            if (candidateWords.Count < config.QuestionCount)
            {
                var extraWords = await _vocabRepo.SearchAsync(
                    take: config.QuestionCount * 2,
                    cancellationToken: cancellationToken);
                candidateWords = candidateWords.Concat(extraWords.Where(ew => !candidateWords.Any(cw => cw.Id == ew.Id))).ToList();
            }
        }
        else
        {
            candidateWords = await _vocabRepo.SearchAsync(
                level: config.Level,
                topicId: config.TopicId,
                examType: config.ExamType,
                take: 100,
                cancellationToken: cancellationToken);

            if (candidateWords.Count < 3)
            {
                // Fallback to all words if filter returned too few
                candidateWords = await _vocabRepo.SearchAsync(take: 100, cancellationToken: cancellationToken);
            }
        }

        if (candidateWords.Count == 0)
        {
            throw new InvalidOperationException("No vocabulary words available to generate a test. Please add words first.");
        }

        // Fetch distractor pool (words with meanings)
        var allPool = await _vocabRepo.SearchAsync(take: 200, cancellationToken: cancellationToken);
        var distractorPool = allPool.Where(w => w.Meanings.Count > 0).ToList();
        if (distractorPool.Count < 4)
        {
            distractorPool = candidateWords.Where(w => w.Meanings.Count > 0).ToList();
        }

        // Shuffle candidate words and pick question count
        int countToTake = (config.CustomWordIds != null && config.CustomWordIds.Count > 0)
            ? Math.Min(config.QuestionCount > 0 ? config.QuestionCount : candidateWords.Count, candidateWords.Count)
            : Math.Min(config.QuestionCount, candidateWords.Count);

        var selectedWords = candidateWords
            .OrderBy(_ => Random.Shared.Next())
            .Take(countToTake)
            .ToList();

        var questions = new List<TestQuestionDto>();
        var testTypes = new[]
        {
            TestType.WordToMeaning,
            TestType.MeaningToWord,
            TestType.FillInTheBlank,
            TestType.PartOfSpeech,
            TestType.ListeningQuiz
        };

        for (int i = 0; i < selectedWords.Count; i++)
        {
            var word = selectedWords[i];
            var qType = config.Type;

            if (qType == TestType.Mixed)
            {
                // Pick appropriate question type
                bool hasExamples = word.Meanings.Any(m => m.Examples.Count > 0);
                bool hasAudio = !string.IsNullOrEmpty(word.AudioUsPath) || !string.IsNullOrEmpty(word.AudioUkPath);

                var availableTypes = new List<TestType> { TestType.WordToMeaning, TestType.MeaningToWord, TestType.PartOfSpeech };
                if (hasExamples) availableTypes.Add(TestType.FillInTheBlank);
                if (hasAudio) availableTypes.Add(TestType.ListeningQuiz);

                qType = availableTypes[Random.Shared.Next(availableTypes.Count)];
            }
            else if (qType == TestType.MultipleChoice)
            {
                qType = TestType.WordToMeaning;
            }

            var question = CreateQuestion(word, qType, i + 1, distractorPool);
            questions.Add(question);
        }

        // Create StudySession in database
        var studySession = new StudySession
        {
            StartedAt = DateTime.UtcNow,
            SessionType = $"Test_{config.Type}",
            TotalCards = questions.Count
        };

        var createdSession = await _reviewRepo.CreateStudySessionAsync(studySession, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cache session in memory for submission validation, pruning stale sessions (> 2 hours)
        var cutoff = DateTime.UtcNow.AddHours(-2);
        foreach (var kvp in ActiveTestSessions)
        {
            if (kvp.Value.CreatedAt < cutoff)
            {
                ActiveTestSessions.TryRemove(kvp.Key, out _);
            }
        }

        ActiveTestSessions[createdSession.Id] = new ActiveSessionEntry(DateTime.UtcNow, questions);

        return new TestSessionDto(
            createdSession.Id,
            config,
            createdSession.StartedAt,
            questions
        );
    }

    public async Task<TestResultDto> SubmitTestAsync(Guid sessionId, List<TestAnswerSubmissionDto> answers, CancellationToken cancellationToken = default)
    {
        ActiveTestSessions.TryGetValue(sessionId, out var entry);
        var cachedQuestions = entry?.Questions;

        var session = await _reviewRepo.GetStudySessionByIdAsync(sessionId, cancellationToken);
        var completedAt = DateTime.UtcNow;

        int correctCount = 0;
        int incorrectCount = 0;
        var evaluatedQuestions = new List<TestQuestionDto>();
        var weakWords = new List<WordDto>();

        if (cachedQuestions != null)
        {
            for (int i = 0; i < cachedQuestions.Count; i++)
            {
                var q = cachedQuestions[i];
                var answerSubmission = answers.FirstOrDefault(a => a.WordId == q.WordId && a.QuestionNumber == q.QuestionNumber);
                string userAnswer = answerSubmission?.SelectedAnswer?.Trim() ?? string.Empty;

                bool isCorrect = string.Equals(userAnswer, q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

                if (isCorrect)
                {
                    correctCount++;
                }
                else
                {
                    incorrectCount++;
                    weakWords.Add(q.TargetWord);
                }

                var updatedQ = q with
                {
                    UserAnswer = userAnswer,
                    IsCorrect = isCorrect
                };
                evaluatedQuestions.Add(updatedQ);

                // Update StudyLog and Card rating if card exists
                var card = await _reviewRepo.GetByWordIdAsync(q.WordId, cancellationToken);
                if (card != null)
                {
                    card.TotalReviews++;
                    if (isCorrect)
                    {
                        card.CorrectReviews++;
                    }
                    else
                    {
                        card.Lapses++;
                        card.Status = LearningStatus.NotRemembered;
                        card.NextReviewAt = DateTime.UtcNow;
                    }
                    card.UpdatedAt = DateTime.UtcNow;
                    await _reviewRepo.UpdateAsync(card, cancellationToken);

                    var log = new StudyLog
                    {
                        StudySessionId = sessionId,
                        ReviewCardId = card.Id,
                        Rating = isCorrect ? ReviewRating.Good : ReviewRating.Again,
                        ResponseTimeMs = answerSubmission?.ResponseTimeMs ?? 0,
                        ResultingStatus = card.Status,
                        ResultingIntervalDays = card.IntervalDays
                    };
                    _reviewRepo.AddStudyLog(log);
                }

                // If user answered incorrectly, auto-sync into Learning Storage as Weak item
                if (!isCorrect && _storageRepo != null)
                {
                    var existingStorage = await _storageRepo.GetByWordIdAsync(q.WordId, cancellationToken);
                    if (existingStorage == null)
                    {
                        var storageItem = new LearningStorageItem
                        {
                            ItemType = StorageItemType.Vocabulary,
                            WordId = q.WordId,
                            Status = StorageLearningStatus.Weak,
                            ReasonAdded = StorageAdditionReason.MultipleErrors,
                            IncorrectCount = 1,
                            EaseFactor = 2.3,
                            IntervalDays = 1,
                            Repetitions = 0,
                            Lapses = 1,
                            RetentionProbability = 40.0,
                            NextReviewAt = DateTime.UtcNow,
                            LastReviewedAt = DateTime.UtcNow
                        };
                        await _storageRepo.AddAsync(storageItem, cancellationToken);
                    }
                    else
                    {
                        existingStorage.IncorrectCount++;
                        existingStorage.Lapses++;
                        existingStorage.Status = StorageLearningStatus.Weak;
                        existingStorage.ReasonAdded = StorageAdditionReason.MultipleErrors;
                        existingStorage.IntervalDays = 1;
                        existingStorage.NextReviewAt = DateTime.UtcNow;
                        existingStorage.RetentionProbability = Math.Max(10.0, existingStorage.RetentionProbability - 20.0);
                        existingStorage.UpdatedAt = DateTime.UtcNow;
                        await _storageRepo.UpdateAsync(existingStorage, cancellationToken);
                    }
                }
            }
        }
        else if (answers.Count > 0)
        {
            // Resilient fallback: Evaluate directly against vocabulary entities when session cache is expired
            var wordIds = answers.Select(a => a.WordId).Where(id => id != Guid.Empty).Distinct().ToList();
            var words = (await _vocabRepo.GetByIdsWithDetailsAsync(wordIds, cancellationToken)).ToDictionary(w => w.Id);

            for (int i = 0; i < answers.Count; i++)
            {
                var ans = answers[i];
                if (!words.TryGetValue(ans.WordId, out var word)) continue;

                var targetWordDto = MapToDto(word);
                var userAnswer = ans.SelectedAnswer?.Trim() ?? string.Empty;

                bool isCorrect = string.Equals(userAnswer, word.Term.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                 word.Meanings.Any(m => string.Equals(userAnswer, m.DefinitionVi.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                                        (!string.IsNullOrWhiteSpace(m.DefinitionEn) && string.Equals(userAnswer, m.DefinitionEn.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (isCorrect)
                {
                    correctCount++;
                }
                else
                {
                    incorrectCount++;
                    weakWords.Add(targetWordDto);
                }

                var primaryMeaning = word.Meanings.FirstOrDefault();
                var correctAnswer = primaryMeaning?.DefinitionVi ?? word.Term;
                var q = new TestQuestionDto(
                    word.Id,
                    ans.QuestionNumber > 0 ? ans.QuestionNumber : i + 1,
                    TestType.WordToMeaning,
                    $"What is the meaning of \"{word.Term}\"?",
                    null,
                    word.AudioUsPath ?? word.AudioUkPath,
                    correctAnswer,
                    new List<string> { correctAnswer, userAnswer },
                    primaryMeaning?.Explanation ?? string.Empty,
                    targetWordDto,
                    userAnswer,
                    isCorrect
                );
                evaluatedQuestions.Add(q);

                var card = await _reviewRepo.GetByWordIdAsync(ans.WordId, cancellationToken);
                if (card != null)
                {
                    card.TotalReviews++;
                    if (isCorrect)
                    {
                        card.CorrectReviews++;
                    }
                    else
                    {
                        card.Lapses++;
                        card.Status = LearningStatus.NotRemembered;
                        card.NextReviewAt = DateTime.UtcNow;
                    }
                    card.UpdatedAt = DateTime.UtcNow;
                    await _reviewRepo.UpdateAsync(card, cancellationToken);

                    var log = new StudyLog
                    {
                        StudySessionId = sessionId,
                        ReviewCardId = card.Id,
                        Rating = isCorrect ? ReviewRating.Good : ReviewRating.Again,
                        ResponseTimeMs = ans.ResponseTimeMs,
                        ResultingStatus = card.Status,
                        ResultingIntervalDays = card.IntervalDays
                    };
                    _reviewRepo.AddStudyLog(log);
                }
            }
        }

        int totalQuestions = evaluatedQuestions.Count > 0 ? evaluatedQuestions.Count : answers.Count;
        double scorePercent = totalQuestions > 0 ? Math.Round((double)correctCount / totalQuestions * 100.0, 1) : 0.0;

        if (session != null)
        {
            session.CompletedAt = completedAt;
            session.TotalCards = totalQuestions;
            session.CorrectCards = correctCount;
            session.IncorrectCards = incorrectCount;
            await _reviewRepo.UpdateStudySessionAsync(session, cancellationToken);
        }

        // Update learning streak using local user date
        await _reviewRepo.UpdateStreakAsync(DateOnly.FromDateTime(DateTime.Now), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (incorrectCount > 0 && _storageRepo != null)
        {
            LearningStorageService.NotifyGlobalStorageChanged();
        }

        // Clean up cache
        ActiveTestSessions.TryRemove(sessionId, out _);

        var startedAt = session?.StartedAt ?? completedAt;
        var timeTaken = completedAt - startedAt;

        var resultType = TestType.Mixed;
        if (session?.SessionType != null && session.SessionType.StartsWith("Test_") &&
            Enum.TryParse<TestType>(session.SessionType.Substring(5), out var parsedType))
        {
            resultType = parsedType;
        }

        var scoreTier = scorePercent >= 85.0 ? TestScoreTier.Excellent
            : scorePercent >= 60.0 ? TestScoreTier.Good
            : TestScoreTier.NeedsPractice;

        var uniqueWeakWords = weakWords.DistinctBy(w => w.Id).ToList();

        return new TestResultDto(
            sessionId,
            resultType,
            startedAt,
            completedAt,
            totalQuestions,
            correctCount,
            incorrectCount,
            scorePercent,
            timeTaken,
            evaluatedQuestions,
            uniqueWeakWords,
            uniqueWeakWords,
            scoreTier
        );
    }

    public async Task<IReadOnlyList<WordDto>> GetWeakWordsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var difficultCards = await _reviewRepo.GetDifficultCardsAsync(limit, cancellationToken);
        var list = new List<WordDto>();

        foreach (var card in difficultCards)
        {
            var word = await _vocabRepo.GetWithDetailsAsync(card.WordId, cancellationToken);
            if (word != null)
            {
                list.Add(MapToDto(word));
            }
        }

        return list;
    }

    private static TestQuestionDto CreateQuestion(
        Word word,
        TestType type,
        int questionNumber,
        List<Word> distractorPool)
    {
        var primaryMeaning = word.Meanings.OrderBy(m => m.OrderIndex).FirstOrDefault();
        var targetPos = primaryMeaning?.PartOfSpeech ?? word.PrimaryPartOfSpeech;
        string definitionVi = primaryMeaning?.DefinitionVi ?? word.Term;
        string? phonetic = word.PhoneticUs ?? word.PhoneticUk;
        string? audio = word.AudioUsPath ?? word.AudioUkPath;

        string prompt;
        string? subPrompt = null;
        string correctAnswer;
        var options = new List<string>();
        string explanation;

        switch (type)
        {
            case TestType.WordToMeaning:
                prompt = $"What is the meaning of \"{word.Term}\"?";
                subPrompt = !string.IsNullOrEmpty(phonetic) ? phonetic : $"Part of speech: {targetPos}";
                correctAnswer = definitionVi;
                explanation = $"{word.Term} ({targetPos}): {definitionVi}";

                options.Add(correctAnswer);
                var samePosMeanings = distractorPool
                    .Where(w => w.Id != word.Id && (w.PrimaryPartOfSpeech == targetPos || w.Meanings.Any(m => m.PartOfSpeech == targetPos)))
                    .SelectMany(w => w.Meanings.Where(m => m.PartOfSpeech == targetPos).DefaultIfEmpty(w.Meanings.FirstOrDefault()))
                    .Where(m => m != null)
                    .Select(m => m!.DefinitionVi)
                    .Where(d => !string.IsNullOrWhiteSpace(d) && !d.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();

                var otherMeanings = distractorPool
                    .Where(w => w.Id != word.Id && w.PrimaryPartOfSpeech != targetPos && !w.Meanings.Any(m => m.PartOfSpeech == targetPos))
                    .SelectMany(w => w.Meanings)
                    .Select(m => m.DefinitionVi)
                    .Where(d => !string.IsNullOrWhiteSpace(d) && !d.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase) && !samePosMeanings.Contains(d))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(Math.Max(0, 3 - samePosMeanings.Count))
                    .ToList();

                options.AddRange(samePosMeanings);
                options.AddRange(otherMeanings);
                break;

            case TestType.MeaningToWord:
                prompt = $"Which word means: \"{definitionVi}\"?";
                subPrompt = $"Part of speech: {targetPos}";
                correctAnswer = word.Term;
                explanation = $"{word.Term} [{phonetic}] — {definitionVi}";

                options.Add(correctAnswer);
                var samePosTerms = distractorPool
                    .Where(w => w.Id != word.Id && (w.PrimaryPartOfSpeech == targetPos || w.Meanings.Any(m => m.PartOfSpeech == targetPos)))
                    .Select(w => w.Term)
                    .Where(t => !t.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();

                var otherTerms = distractorPool
                    .Where(w => w.Id != word.Id && w.PrimaryPartOfSpeech != targetPos && !w.Meanings.Any(m => m.PartOfSpeech == targetPos))
                    .Select(w => w.Term)
                    .Where(t => !t.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase) && !samePosTerms.Contains(t))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(Math.Max(0, 3 - samePosTerms.Count))
                    .ToList();

                options.AddRange(samePosTerms);
                options.AddRange(otherTerms);
                break;

            case TestType.FillInTheBlank:
                var example = word.Meanings.SelectMany(m => m.Examples).FirstOrDefault();
                if (example != null && !string.IsNullOrWhiteSpace(example.SentenceEn))
                {
                    // Blank out the word (case-insensitive)
                    string pattern = $@"\b{Regex.Escape(word.Term)}\b";
                    string blanked = Regex.Replace(example.SentenceEn, pattern, "_______", RegexOptions.IgnoreCase);

                    // If pattern didn't match directly, replace word term if present
                    if (!blanked.Contains("_______") && example.SentenceEn.Contains(word.Term, StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = example.SentenceEn.IndexOf(word.Term, StringComparison.OrdinalIgnoreCase);
                        blanked = example.SentenceEn.Remove(idx, word.Term.Length).Insert(idx, "_______");
                    }

                    prompt = $"Fill in the missing word:\n\"{blanked}\"";
                    subPrompt = $"Translation: \"{example.SentenceVi}\"";
                    correctAnswer = word.Term;
                    explanation = $"Complete sentence: {example.SentenceEn} ({example.SentenceVi})";
                }
                else
                {
                    prompt = $"Fill in the blank for definition: \"{definitionVi}\"";
                    subPrompt = $"Part of speech: {word.PrimaryPartOfSpeech}";
                    correctAnswer = word.Term;
                    explanation = $"{word.Term}: {definitionVi}";
                }

                options.Add(correctAnswer);
                var samePosBlankWords = distractorPool
                    .Where(w => w.Id != word.Id && w.PrimaryPartOfSpeech == word.PrimaryPartOfSpeech)
                    .Select(w => w.Term)
                    .Where(t => !t.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();

                var otherBlankWords = distractorPool
                    .Where(w => w.Id != word.Id && w.PrimaryPartOfSpeech != word.PrimaryPartOfSpeech)
                    .Select(w => w.Term)
                    .Where(t => !t.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase) && !samePosBlankWords.Contains(t))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(Math.Max(0, 3 - samePosBlankWords.Count))
                    .ToList();

                options.AddRange(samePosBlankWords);
                options.AddRange(otherBlankWords);
                break;

            case TestType.PartOfSpeech:
                prompt = $"What is the primary part of speech of \"{word.Term}\"?";
                subPrompt = $"Meaning: {definitionVi}";
                correctAnswer = word.PrimaryPartOfSpeech.ToString();
                explanation = $"\"{word.Term}\" is a {word.PrimaryPartOfSpeech} ({definitionVi}).";

                options.Add(correctAnswer);
                var allPos = Enum.GetValues<PartOfSpeech>()
                    .Where(pos => pos != word.PrimaryPartOfSpeech)
                    .Select(pos => pos.ToString())
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(allPos);
                break;

            case TestType.ListeningQuiz:
                prompt = "Listen carefully to the pronunciation and choose the correct word:";
                subPrompt = $"Meaning clue: {definitionVi}";
                correctAnswer = word.Term;
                explanation = $"{word.Term} [{phonetic}] — {definitionVi}";

                options.Add(correctAnswer);
                var wrongAudioTerms = distractorPool
                    .Where(w => w.Id != word.Id)
                    .Select(w => w.Term)
                    .Where(t => !t.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(wrongAudioTerms);
                break;

            default:
                prompt = $"Select the matching definition for \"{word.Term}\":";
                subPrompt = phonetic;
                correctAnswer = definitionVi;
                explanation = $"{word.Term}: {definitionVi}";

                options.Add(correctAnswer);
                var defChoices = distractorPool
                    .Where(w => w.Id != word.Id)
                    .SelectMany(w => w.Meanings)
                    .Select(m => m.DefinitionVi)
                    .Where(d => !string.IsNullOrWhiteSpace(d) && !d.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(defChoices);
                break;
        }

        // Deduplicate in case distractors had duplicates
        options = options.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Fallback in case distractor pool had fewer than 3 unique options
        while (options.Count < 4)
        {
            options.Add($"Option {options.Count + 1}");
        }

        // Shuffle options so correct answer is at a random index
        var shuffledOptions = options.OrderBy(_ => Random.Shared.Next()).ToList();

        return new TestQuestionDto(
            word.Id,
            questionNumber,
            type,
            prompt,
            subPrompt,
            audio,
            correctAnswer,
            shuffledOptions,
            explanation,
            MapToDto(word)
        );
    }

    private static WordDto MapToDto(Word w)
    {
        var meanings = w.Meanings
            .OrderBy(m => m.OrderIndex)
            .Select(m => new WordMeaningDto(
                m.Id,
                m.PartOfSpeech,
                m.DefinitionVi,
                m.DefinitionEn,
                m.Explanation,
                m.OrderIndex,
                m.Examples.Select(e => new ExampleSentenceDto(e.Id, e.SentenceEn, e.SentenceVi, e.AudioPath)).ToList()
            )).ToList();

        var topics = w.WordTopics.Select(wt => wt.Topic?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList();
        var examTags = w.WordExamTags.Select(et => et.ExamTag?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList();

        return new WordDto(
            w.Id,
            w.Term,
            w.PhoneticUk,
            w.PhoneticUs,
            w.AudioUkPath,
            w.AudioUsPath,
            w.Level,
            w.PrimaryPartOfSpeech,
            w.Notes,
            meanings,
            topics,
            examTags,
            w.ReviewCard?.Status,
            w.ReviewCard?.NextReviewAt,
            w.ReviewCard?.Confidence ?? 0.0
        );
    }
}
