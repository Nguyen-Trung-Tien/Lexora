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

    // Cache of active in-memory test questions for scoring
    private static readonly Dictionary<Guid, List<TestQuestionDto>> ActiveTestSessions = new();

    public TestService(
        IVocabularyRepository vocabRepo,
        IReviewRepository reviewRepo,
        IUnitOfWork unitOfWork)
    {
        _vocabRepo = vocabRepo;
        _reviewRepo = reviewRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<TestSessionDto> GenerateTestAsync(TestConfigurationDto config, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Word> candidateWords;

        if (config.WeakWordsOnly)
        {
            var difficultCards = await _reviewRepo.GetDifficultCardsAsync(50, cancellationToken);
            var weakWordIds = difficultCards.Select(c => c.WordId).ToList();

            var loadedWords = new List<Word>();
            foreach (var id in weakWordIds)
            {
                var w = await _vocabRepo.GetWithDetailsAsync(id, cancellationToken);
                if (w != null) loadedWords.Add(w);
            }

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
        var selectedWords = candidateWords
            .OrderBy(_ => Random.Shared.Next())
            .Take(config.QuestionCount)
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

        // Cache session in memory for submission validation
        lock (ActiveTestSessions)
        {
            ActiveTestSessions[createdSession.Id] = questions;
        }

        return new TestSessionDto(
            createdSession.Id,
            config,
            createdSession.StartedAt,
            questions
        );
    }

    public async Task<TestResultDto> SubmitTestAsync(Guid sessionId, List<TestAnswerSubmissionDto> answers, CancellationToken cancellationToken = default)
    {
        List<TestQuestionDto>? cachedQuestions;
        lock (ActiveTestSessions)
        {
            ActiveTestSessions.TryGetValue(sessionId, out cachedQuestions);
        }

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
                    if (!isCorrect)
                    {
                        card.Lapses++;
                        card.Status = LearningStatus.NotRemembered;
                        card.NextReviewAt = DateTime.UtcNow;
                    }

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

        // Update learning streak
        await _reviewRepo.UpdateStreakAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Clean up cache
        lock (ActiveTestSessions)
        {
            ActiveTestSessions.Remove(sessionId);
        }

        var startedAt = session?.StartedAt ?? completedAt;
        var timeTaken = completedAt - startedAt;

        var resultType = TestType.Mixed;
        if (session?.SessionType != null && session.SessionType.StartsWith("Test_") &&
            Enum.TryParse<TestType>(session.SessionType.Substring(5), out var parsedType))
        {
            resultType = parsedType;
        }

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
            weakWords.DistinctBy(w => w.Id).ToList(),
            weakWords.DistinctBy(w => w.Id).ToList()
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
                subPrompt = !string.IsNullOrEmpty(phonetic) ? phonetic : $"Part of speech: {word.PrimaryPartOfSpeech}";
                correctAnswer = definitionVi;
                explanation = $"{word.Term} ({word.PrimaryPartOfSpeech}): {definitionVi}";

                options.Add(correctAnswer);
                var wrongMeanings = distractorPool
                    .Where(w => w.Id != word.Id)
                    .SelectMany(w => w.Meanings)
                    .Select(m => m.DefinitionVi)
                    .Where(d => !string.IsNullOrWhiteSpace(d) && !d.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(wrongMeanings);
                break;

            case TestType.MeaningToWord:
                prompt = $"Which word means: \"{definitionVi}\"?";
                subPrompt = $"Part of speech: {word.PrimaryPartOfSpeech}";
                correctAnswer = word.Term;
                explanation = $"{word.Term} [{phonetic}] — {definitionVi}";

                options.Add(correctAnswer);
                var wrongTerms = distractorPool
                    .Where(w => w.Id != word.Id)
                    .Select(w => w.Term)
                    .Where(t => !t.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(wrongTerms);
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
                var wrongWords = distractorPool
                    .Where(w => w.Id != word.Id)
                    .Select(w => w.Term)
                    .Where(t => !t.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(3)
                    .ToList();
                options.AddRange(wrongWords);
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
