using Lexora.Application.Common;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services;

public partial class ExportImportService
{
    public async Task<ImportResult> ImportSmartAsync(
        string content,
        string? fileNameHint = null,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" }, 0);
        }

        var format = SmartContentDetector.DetectFormat(content, fileNameHint);
        return format switch
        {
            DataFormat.Json => await ImportFromJsonAsync(content, conflictResolution, cancellationToken, progress),
            DataFormat.Csv => await ImportFromCsvAsync(content, conflictResolution, cancellationToken, progress),
            _ => await ImportFromTxtAsync(content, conflictResolution, cancellationToken, progress)
        };
    }

    public async Task<SmartImportCommitResultDto> CommitSmartImportAsync(
        SmartImportCommitRequestDto request,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int updated = 0;
        int skipped = 0;
        var errors = new List<string>();

        var selectedItems = request.Items.Where(i => i.IsSelected && i.IsValid).ToList();
        if (selectedItems.Count == 0)
        {
            return new SmartImportCommitResultDto(0, 0, 0, new List<string> { "Không có mục hợp lệ nào được chọn / No valid items selected." });
        }

        var throttler = new ProgressThrottler(progress);
        throttler.Report(5, 0, selectedItems.Count, "Chuẩn bị giao dịch nhập dữ liệu...", "Preparing transaction...", force: true);

        // 1. In-batch de-duplication
        var processedItems = new List<SmartImportItemDto>();
        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in selectedItems)
        {
            var norm = NormalizeTerm(item.Term);
            if (string.IsNullOrEmpty(norm)) continue;

            if (request.ConflictResolution == ImportConflictResolution.Skip)
            {
                if (seenInBatch.Add(norm))
                {
                    processedItems.Add(item);
                }
                else
                {
                    skipped++;
                }
            }
            else // Overwrite
            {
                var existingIdx = processedItems.FindIndex(p => NormalizeTerm(p.Term) == norm);
                if (existingIdx >= 0)
                {
                    var existingItem = processedItems[existingIdx];
                    var combinedMeanings = new List<WordMeaningCreateDto>();
                    if (existingItem.RichMeanings != null) combinedMeanings.AddRange(existingItem.RichMeanings);
                    else
                    {
                        combinedMeanings.Add(new WordMeaningCreateDto(
                            existingItem.PartOfSpeech != 0 ? existingItem.PartOfSpeech : PartOfSpeech.Noun,
                            existingItem.DefinitionVi,
                            existingItem.DefinitionEn,
                            null,
                            !string.IsNullOrWhiteSpace(existingItem.Examples)
                                ? [new ExampleSentenceCreateDto(existingItem.Examples, string.Empty, null)]
                                : []));
                    }

                    if (item.RichMeanings != null) combinedMeanings.AddRange(item.RichMeanings);
                    else
                    {
                        combinedMeanings.Add(new WordMeaningCreateDto(
                            item.PartOfSpeech != 0 ? item.PartOfSpeech : PartOfSpeech.Noun,
                            item.DefinitionVi,
                            item.DefinitionEn,
                            null,
                            !string.IsNullOrWhiteSpace(item.Examples)
                                ? [new ExampleSentenceCreateDto(item.Examples, string.Empty, null)]
                                : []));
                    }

                    var allPos = combinedMeanings.Select(m => m.PartOfSpeech).Distinct().ToList();

                    processedItems[existingIdx] = existingItem with
                    {
                        Level = item.Level != CefrLevel.None ? item.Level : existingItem.Level,
                        PhoneticUk = item.PhoneticUk ?? existingItem.PhoneticUk,
                        PhoneticUs = item.PhoneticUs ?? existingItem.PhoneticUs,
                        Examples = string.Join("; ", new[] { existingItem.Examples, item.Examples }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        Topics = string.Join("; ", new[] { existingItem.Topics, item.Topics }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        ExamTags = string.Join("; ", new[] { existingItem.ExamTags, item.ExamTags }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        RichMeanings = combinedMeanings,
                        AllPartsOfSpeech = allPos
                    };
                    updated++;
                }
                else
                {
                    seenInBatch.Add(norm);
                    processedItems.Add(item);
                }
            }
        }

        int total = processedItems.Count;
        if (total == 0)
        {
            return new SmartImportCommitResultDto(0, 0, skipped, errors);
        }

        // 2. Pre-fetch existing words from SQLite DB for all distinct terms in batch
        var distinctTerms = processedItems.Select(p => NormalizeTerm(p.Term)).Distinct().ToList();
        IReadOnlyDictionary<string, Word>? existingWordsMap = null;
        try
        {
            existingWordsMap = await _vocabRepo.GetWordsByTermsWithDetailsAsync(distinctTerms, cancellationToken);
        }
        catch
        {
            existingWordsMap = null;
        }

        // 3. Pre-fetch topics & exam tags (resilient lookup against duplicate DB records)
        var rawTopics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
        var allTopics = BuildTopicLookup(rawTopics);

        var rawTags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
        var allExamTags = BuildExamTagLookup(rawTags);

        // 4. Begin single atomic transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            int current = 0;
            foreach (var item in processedItems)
            {
                current++;
                cancellationToken.ThrowIfCancellationRequested();
                var norm = NormalizeTerm(item.Term);

                Word? existing = null;
                if (existingWordsMap != null)
                {
                    existingWordsMap.TryGetValue(norm, out existing);
                }
                else
                {
                    existing = await _vocabRepo.GetByTermAsync(item.Term, cancellationToken);
                }

                if (existing != null)
                {
                    if (request.ConflictResolution == ImportConflictResolution.Overwrite)
                    {
                        if (item.Level != CefrLevel.None) existing.Level = item.Level;
                        if (!string.IsNullOrWhiteSpace(item.PhoneticUk)) existing.PhoneticUk = SafeTruncateNullable(item.PhoneticUk, 100);
                        if (!string.IsNullOrWhiteSpace(item.PhoneticUs)) existing.PhoneticUs = SafeTruncateNullable(item.PhoneticUs, 100);
                        if (!string.IsNullOrWhiteSpace(item.Notes)) existing.Notes = SafeTruncateNullable(item.Notes, 2000);
                        if (item.PartOfSpeech != 0) existing.PrimaryPartOfSpeech = item.PartOfSpeech;
                        existing.UpdatedAt = DateTime.UtcNow;

                        _vocabRepo.RemoveWordChildren(existing);

                        var newMeanings = BuildWordMeanings(existing.Id, item);
                        foreach (var m in newMeanings)
                        {
                            existing.Meanings.Add(m);
                        }
                        if (newMeanings.Count > 0)
                        {
                            _vocabRepo.AddMeanings(newMeanings);
                        }

                        if (!string.IsNullOrWhiteSpace(item.Topics))
                        {
                            var tTokens = item.Topics.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            var newTopics = new List<WordTopic>();
                            foreach (var tname in tTokens)
                            {
                                var tlower = tname.ToLowerInvariant();
                                if (!allTopics.TryGetValue(tlower, out var top))
                                {
                                    top = new Topic { Name = SafeTruncate(tname, 100) };
                                    await _vocabRepo.AddTopicAsync(top, cancellationToken);
                                    allTopics[tlower] = top;
                                }
                                if (!existing.WordTopics.Any(wt => wt.TopicId == top.Id))
                                {
                                    var wt = new WordTopic { WordId = existing.Id, TopicId = top.Id };
                                    existing.WordTopics.Add(wt);
                                    newTopics.Add(wt);
                                }
                            }
                            if (newTopics.Count > 0)
                            {
                                _vocabRepo.AddWordTopics(newTopics);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(item.ExamTags))
                        {
                            var etTokens = item.ExamTags.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            var newTags = new List<WordExamTag>();
                            foreach (var etname in etTokens)
                            {
                                var etlower = etname.ToLowerInvariant();
                                if (!allExamTags.TryGetValue(etlower, out var et))
                                {
                                    var examType = ExamType.General;
                                    if (etlower.Contains("toeic")) examType = ExamType.Toeic;
                                    else if (etlower.Contains("ielts")) examType = ExamType.Ielts;
                                    else if (etlower.Contains("vstep")) examType = ExamType.Vstep;

                                    et = new ExamTag { Name = SafeTruncate(etname, 100), Type = examType };
                                    await _vocabRepo.AddExamTagAsync(et, cancellationToken);
                                    allExamTags[etlower] = et;
                                }
                                if (!existing.WordExamTags.Any(wet => wet.ExamTagId == et.Id))
                                {
                                    var wet = new WordExamTag { WordId = existing.Id, ExamTagId = et.Id };
                                    existing.WordExamTags.Add(wet);
                                    newTags.Add(wet);
                                }
                            }
                            if (newTags.Count > 0)
                            {
                                _vocabRepo.AddWordExamTags(newTags);
                            }
                        }

                        await _vocabRepo.UpdateAsync(existing, cancellationToken);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                    continue;
                }

                var word = new Word
                {
                    Term = SafeTruncate(item.Term, 150),
                    NormalizedTerm = norm,
                    PhoneticUk = SafeTruncateNullable(item.PhoneticUk, 100),
                    PhoneticUs = SafeTruncateNullable(item.PhoneticUs, 100),
                    Level = item.Level,
                    PrimaryPartOfSpeech = item.PartOfSpeech != 0 ? item.PartOfSpeech : PartOfSpeech.Noun,
                    Notes = SafeTruncateNullable(item.Notes, 2000),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var meanings = BuildWordMeanings(word.Id, item);
                foreach (var m in meanings)
                {
                    word.Meanings.Add(m);
                }

                if (!string.IsNullOrWhiteSpace(item.Topics))
                {
                    var tTokens = item.Topics.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var tname in tTokens)
                    {
                        var tlower = tname.ToLowerInvariant();
                        if (!allTopics.TryGetValue(tlower, out var top))
                        {
                            top = new Topic { Name = SafeTruncate(tname, 100) };
                            await _vocabRepo.AddTopicAsync(top, cancellationToken);
                            allTopics[tlower] = top;
                        }
                        if (!word.WordTopics.Any(wt => wt.TopicId == top.Id))
                        {
                            word.WordTopics.Add(new WordTopic { WordId = word.Id, TopicId = top.Id });
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(item.ExamTags))
                {
                    var etTokens = item.ExamTags.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var etname in etTokens)
                    {
                        var etlower = etname.ToLowerInvariant();
                        if (!allExamTags.TryGetValue(etlower, out var et))
                        {
                            var examType = ExamType.General;
                            if (etlower.Contains("toeic")) examType = ExamType.Toeic;
                            else if (etlower.Contains("ielts")) examType = ExamType.Ielts;
                            else if (etlower.Contains("vstep")) examType = ExamType.Vstep;

                            et = new ExamTag { Name = SafeTruncate(etname, 100), Type = examType };
                            await _vocabRepo.AddExamTagAsync(et, cancellationToken);
                            allExamTags[etlower] = et;
                        }
                        if (!word.WordExamTags.Any(wet => wet.ExamTagId == et.Id))
                        {
                            word.WordExamTags.Add(new WordExamTag { WordId = word.Id, ExamTagId = et.Id });
                        }
                    }
                }

                word.ReviewCard = new ReviewCard
                {
                    WordId = word.Id,
                    Status = LearningStatus.New,
                    NextReviewAt = DateTime.UtcNow
                };

                await _vocabRepo.AddAsync(word, cancellationToken);
                imported++;

                int pct = (int)((double)current / total * 85) + 10;
                throttler.Report(pct, current, total, $"Đang nhập: {current} / {total}", item.Term);
            }

            throttler.Report(95, total, total, "Đang lưu thay đổi vào cơ sở dữ liệu...", "Saving transaction...", force: true);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            _unitOfWork.ClearTracker();
            throttler.Report(100, total, total, "Hoàn tất nhập dữ liệu!", "Import complete.", force: true);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        return new SmartImportCommitResultDto(imported, updated, skipped, errors);
    }

    public async Task<ImportResult> ImportFromJsonAsync(
        string jsonContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int updated = 0;
        int skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new ImportResult(0, 0, new List<string> { "JSON data is empty" }, 0);
        }

        if (jsonContent.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" }, 0);
        }

        var trimmedJson = jsonContent.Trim();
        if (trimmedJson.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = trimmedJson.IndexOf('\n');
            if (firstNl >= 0)
            {
                trimmedJson = trimmedJson.Substring(firstNl + 1);
            }
            var lastFence = trimmedJson.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
            {
                trimmedJson = trimmedJson.Substring(0, lastFence);
            }
            trimmedJson = trimmedJson.Trim();
        }

        var throttler = new ProgressThrottler(progress);
        throttler.Report(5, 0, 0, "Đang phân tích cấu trúc JSON...", "Parsing JSON buffer...", force: true);

        try
        {
            var list = DeserializeWordsJson(trimmedJson);
            if (list == null || list.Count == 0)
            {
                return new ImportResult(0, 0, new List<string> { "JSON data is empty or invalid" }, 0);
            }

            int total = list.Count;
            throttler.Report(10, 0, total, $"Đã tìm thấy {total} từ vựng. Đang chuẩn bị cơ sở dữ liệu...", "Querying metadata...", force: true);

            var rawTopics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
            var allTopics = BuildTopicLookup(rawTopics);

            var rawTags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
            var allExamTags = BuildExamTagLookup(rawTags);

            HashSet<string>? existingTermsSet = null;
            if (conflictResolution == ImportConflictResolution.Skip)
            {
                try
                {
                    existingTermsSet = await _vocabRepo.GetAllNormalizedTermsAsync(cancellationToken);
                }
                catch
                {
                    existingTermsSet = null;
                }
            }

            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                int current = 0;
                for (int chunkStart = 0; chunkStart < total; chunkStart += ImportBatchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var chunk = list.Skip(chunkStart).Take(ImportBatchSize).ToList();

                    IReadOnlyDictionary<string, Word>? existingWordsMap = null;
                    if (conflictResolution == ImportConflictResolution.Overwrite)
                    {
                        var normalizedTerms = chunk.Select(x => x.ResolvedTerm.Trim().ToLowerInvariant()).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
                        try
                        {
                            existingWordsMap = await _vocabRepo.GetWordsByTermsWithDetailsAsync(normalizedTerms, cancellationToken);
                        }
                        catch
                        {
                            existingWordsMap = null;
                        }
                    }

                    foreach (var item in chunk)
                    {
                        current++;
                        string term = item.ResolvedTerm;
                        if (string.IsNullOrWhiteSpace(term))
                        {
                            skipped++;
                            continue;
                        }

                        var normalized = NormalizeTerm(term);

                        if (!seenTerms.Add(normalized))
                        {
                            skipped++;
                            continue;
                        }

                        if (conflictResolution == ImportConflictResolution.Skip && existingTermsSet != null && existingTermsSet.Contains(normalized))
                        {
                            skipped++;
                            continue;
                        }

                        Word? existing = null;
                        if (conflictResolution == ImportConflictResolution.Overwrite || existingTermsSet == null)
                        {
                            if (existingWordsMap != null)
                            {
                                existingWordsMap.TryGetValue(normalized, out existing);
                            }
                            else
                            {
                                existing = await _vocabRepo.GetByTermAsync(normalized, cancellationToken);
                            }
                        }

                        if (existing != null)
                        {
                            if (conflictResolution == ImportConflictResolution.Overwrite)
                            {
                                if (item.Level != CefrLevel.None) existing.Level = item.Level;
                                if (!string.IsNullOrWhiteSpace(item.ResolvedPhoneticUk)) existing.PhoneticUk = item.ResolvedPhoneticUk;
                                if (!string.IsNullOrWhiteSpace(item.ResolvedPhoneticUs)) existing.PhoneticUs = item.ResolvedPhoneticUs;
                                if (!string.IsNullOrWhiteSpace(item.Notes)) existing.Notes = item.Notes;
                                if (item.PrimaryPartOfSpeech != 0) existing.PrimaryPartOfSpeech = item.PrimaryPartOfSpeech;
                                existing.UpdatedAt = DateTime.UtcNow;

                                _vocabRepo.RemoveWordChildren(existing);

                                int mIdx = 0;
                                var newMeanings = new List<WordMeaning>();
                                foreach (var m in item.Meanings)
                                {
                                    var meaning = new WordMeaning
                                    {
                                        WordId = existing.Id,
                                        PartOfSpeech = m.PartOfSpeech != 0 ? m.PartOfSpeech : existing.PrimaryPartOfSpeech,
                                        DefinitionVi = m.ResolvedDefinitionVi,
                                        DefinitionEn = m.ResolvedDefinitionEn,
                                        Explanation = m.Explanation,
                                        OrderIndex = mIdx++
                                    };

                                    foreach (var ex in m.Examples)
                                    {
                                        if (!string.IsNullOrWhiteSpace(ex.ResolvedSentenceEn))
                                        {
                                            meaning.Examples.Add(new ExampleSentence
                                            {
                                                WordMeaningId = meaning.Id,
                                                SentenceEn = ex.ResolvedSentenceEn,
                                                SentenceVi = ex.ResolvedSentenceVi
                                            });
                                        }
                                    }

                                    newMeanings.Add(meaning);
                                    existing.Meanings.Add(meaning);
                                }

                                if (existing.Meanings.Count == 0 && !string.IsNullOrWhiteSpace(item.ResolvedDefinitionVi))
                                {
                                    var meaning = new WordMeaning
                                    {
                                        WordId = existing.Id,
                                        PartOfSpeech = existing.PrimaryPartOfSpeech,
                                        DefinitionVi = item.ResolvedDefinitionVi,
                                        DefinitionEn = item.ResolvedDefinitionEn,
                                        OrderIndex = 0
                                    };
                                    newMeanings.Add(meaning);
                                    existing.Meanings.Add(meaning);
                                }

                                if (newMeanings.Count > 0)
                                {
                                    _vocabRepo.AddMeanings(newMeanings);
                                }

                                var newTopics = new List<WordTopic>();
                                foreach (var tname in item.ResolvedTopics)
                                {
                                    var lower = tname.Trim().ToLowerInvariant();
                                    if (!allTopics.TryGetValue(lower, out var topic))
                                    {
                                        topic = new Topic { Name = tname.Trim() };
                                        await _vocabRepo.AddTopicAsync(topic, cancellationToken);
                                        allTopics[lower] = topic;
                                    }
                                    if (!existing.WordTopics.Any(wt => wt.TopicId == topic.Id))
                                    {
                                        var wt = new WordTopic { WordId = existing.Id, TopicId = topic.Id };
                                        existing.WordTopics.Add(wt);
                                        newTopics.Add(wt);
                                    }
                                }
                                if (newTopics.Count > 0)
                                {
                                    _vocabRepo.AddWordTopics(newTopics);
                                }

                                var newTags = new List<WordExamTag>();
                                foreach (var etname in item.ResolvedExamTags)
                                {
                                    var lower = etname.Trim().ToLowerInvariant();
                                    if (!allExamTags.TryGetValue(lower, out var examTag))
                                    {
                                        var examType = ExamType.General;
                                        if (lower.Contains("toeic")) examType = ExamType.Toeic;
                                        else if (lower.Contains("ielts")) examType = ExamType.Ielts;
                                        else if (lower.Contains("vstep")) examType = ExamType.Vstep;

                                        examTag = new ExamTag { Name = etname.Trim(), Type = examType };
                                        await _vocabRepo.AddExamTagAsync(examTag, cancellationToken);
                                        allExamTags[lower] = examTag;
                                    }
                                    if (!existing.WordExamTags.Any(wet => wet.ExamTagId == examTag.Id))
                                    {
                                        var wet = new WordExamTag { WordId = existing.Id, ExamTagId = examTag.Id };
                                        existing.WordExamTags.Add(wet);
                                        newTags.Add(wet);
                                    }
                                }
                                if (newTags.Count > 0)
                                {
                                    _vocabRepo.AddWordExamTags(newTags);
                                }

                                await _vocabRepo.UpdateAsync(existing, cancellationToken);
                                updated++;
                            }
                            else
                            {
                                skipped++;
                            }
                            continue;
                        }

                        var primaryPos = item.PrimaryPartOfSpeech != 0
                            ? item.PrimaryPartOfSpeech
                            : (item.Meanings.FirstOrDefault(m => m.PartOfSpeech != 0)?.PartOfSpeech ?? (!string.IsNullOrWhiteSpace(item.Pos) ? ParsePartOfSpeech(item.Pos) : PartOfSpeech.Noun));

                        var word = new Word
                        {
                            Term = term,
                            NormalizedTerm = normalized,
                            PhoneticUk = item.ResolvedPhoneticUk,
                            PhoneticUs = item.ResolvedPhoneticUs,
                            Level = item.Level,
                            PrimaryPartOfSpeech = primaryPos,
                            Notes = item.Notes
                        };

                        int idx = 0;
                        foreach (var m in item.Meanings)
                        {
                            var meaningPos = m.PartOfSpeech != 0 ? m.PartOfSpeech : (!string.IsNullOrWhiteSpace(m.Pos) ? ParsePartOfSpeech(m.Pos) : primaryPos);
                            var meaning = new WordMeaning
                            {
                                WordId = word.Id,
                                PartOfSpeech = meaningPos,
                                DefinitionVi = m.ResolvedDefinitionVi,
                                DefinitionEn = m.ResolvedDefinitionEn,
                                Explanation = m.Explanation,
                                OrderIndex = idx++
                            };

                            foreach (var ex in m.Examples)
                            {
                                if (!string.IsNullOrWhiteSpace(ex.ResolvedSentenceEn))
                                {
                                    meaning.Examples.Add(new ExampleSentence
                                    {
                                        WordMeaningId = meaning.Id,
                                        SentenceEn = ex.ResolvedSentenceEn,
                                        SentenceVi = ex.ResolvedSentenceVi
                                    });
                                }
                            }

                            word.Meanings.Add(meaning);
                        }

                        if (word.Meanings.Count == 0 && !string.IsNullOrWhiteSpace(item.ResolvedDefinitionVi))
                        {
                            word.Meanings.Add(new WordMeaning
                            {
                                WordId = word.Id,
                                PartOfSpeech = word.PrimaryPartOfSpeech,
                                DefinitionVi = item.ResolvedDefinitionVi,
                                DefinitionEn = item.ResolvedDefinitionEn,
                                OrderIndex = 0
                            });
                        }
                        else if (word.Meanings.Count == 0)
                        {
                            word.Meanings.Add(new WordMeaning
                            {
                                WordId = word.Id,
                                PartOfSpeech = word.PrimaryPartOfSpeech,
                                DefinitionVi = term,
                                OrderIndex = 0
                            });
                        }

                        foreach (var tname in item.ResolvedTopics)
                        {
                            var lower = tname.Trim().ToLowerInvariant();
                            if (!allTopics.TryGetValue(lower, out var topic))
                            {
                                topic = new Topic { Name = tname.Trim() };
                                await _vocabRepo.AddTopicAsync(topic, cancellationToken);
                                allTopics[lower] = topic;
                            }
                            word.WordTopics.Add(new WordTopic { WordId = word.Id, TopicId = topic.Id });
                        }

                        foreach (var etname in item.ResolvedExamTags)
                        {
                            var lower = etname.Trim().ToLowerInvariant();
                            if (!allExamTags.TryGetValue(lower, out var examTag))
                            {
                                var examType = ExamType.General;
                                if (lower.Contains("toeic")) examType = ExamType.Toeic;
                                else if (lower.Contains("ielts")) examType = ExamType.Ielts;
                                else if (lower.Contains("vstep")) examType = ExamType.Vstep;

                                examTag = new ExamTag { Name = etname.Trim(), Type = examType };
                                await _vocabRepo.AddExamTagAsync(examTag, cancellationToken);
                                allExamTags[lower] = examTag;
                            }
                            word.WordExamTags.Add(new WordExamTag { WordId = word.Id, ExamTagId = examTag.Id });
                        }

                        word.ReviewCard = new ReviewCard
                        {
                            WordId = word.Id,
                            Status = LearningStatus.New,
                            NextReviewAt = DateTime.UtcNow
                        };

                        await _vocabRepo.AddAsync(word, cancellationToken);
                        imported++;
                    }

                    if (chunkStart + ImportBatchSize < total)
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        _unitOfWork.ClearTracker();
                    }

                    int pct = (int)((double)current / total * 80) + 15;
                    throttler.Report(pct, current, total, $"Đang xử lý: {current} / {total}", chunk[^1].ResolvedTerm);
                }

                throttler.Report(95, total, total, "Đang ghi dữ liệu vào cơ sở dữ liệu...", "Committing transaction...", force: true);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                _unitOfWork.ClearTracker();
                throttler.Report(100, total, total, "Nhập dữ liệu hoàn tất!", "Complete.", force: true);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing JSON: {ex.Message}");
        }

        return new ImportResult(imported, skipped, errors, updated);
    }

    public async Task<ImportResult> ImportFromCsvAsync(
        string csvContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int updated = 0;
        int skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return new ImportResult(0, 0, new List<string> { "CSV content is empty" }, 0);
        }

        if (csvContent.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" }, 0);
        }

        var throttler = new ProgressThrottler(progress);

        try
        {
            throttler.Report(5, 0, 0, "Đang phân tích cấu trúc CSV...", "Parsing CSV rows...", force: true);
            var records = ParseCsvRecords(csvContent);
            if (records.Count <= 1)
            {
                return new ImportResult(0, 0, new List<string> { "CSV content has no data rows" }, 0);
            }

            int total = records.Count - 1;
            throttler.Report(10, 0, total, $"Đã tìm thấy {total} dòng từ vựng...", "Querying metadata...", force: true);

            var rawTopics = await _vocabRepo.GetAllTopicsAsync(cancellationToken);
            var allTopics = BuildTopicLookup(rawTopics);

            var rawTags = await _vocabRepo.GetAllExamTagsAsync(cancellationToken);
            var allExamTags = BuildExamTagLookup(rawTags);

            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dataRows = records.Skip(1).ToList();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                for (int chunkStart = 0; chunkStart < dataRows.Count; chunkStart += ImportBatchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var chunk = dataRows.Skip(chunkStart).Take(ImportBatchSize).ToList();

                    var chunkTerms = chunk
                        .Where(cols => cols.Count > 0 && !string.IsNullOrWhiteSpace(cols[0]))
                        .Select(cols => cols[0].Trim().ToLowerInvariant())
                        .Distinct()
                        .ToList();

                    IReadOnlyDictionary<string, Word>? existingDict = null;
                    try
                    {
                        existingDict = await _vocabRepo.GetWordsByTermsWithDetailsAsync(chunkTerms, cancellationToken);
                    }
                    catch
                    {
                        existingDict = null;
                    }

                    if (existingDict == null)
                    {
                        var fallbackDict = new Dictionary<string, Word>(StringComparer.OrdinalIgnoreCase);
                        foreach (var termKey in chunkTerms)
                        {
                            var w = await _vocabRepo.GetByTermAsync(termKey, cancellationToken);
                            if (w != null) fallbackDict[termKey] = w;
                        }
                        existingDict = fallbackDict;
                    }

                    int currentCount = chunkStart;
                    foreach (var columns in chunk)
                    {
                        currentCount++;
                        if (columns.Count == 0 || string.IsNullOrWhiteSpace(columns[0]))
                        {
                            skipped++;
                            continue;
                        }

                        string term = columns[0].Trim();
                        string normalized = NormalizeTerm(term);

                        if (!seenTerms.Add(normalized))
                        {
                            skipped++;
                            continue;
                        }

                        CefrLevel level = CefrLevel.None;
                        if (columns.Count > 1)
                        {
                            level = ParseCefrLevel(columns[1].Trim(), CefrLevel.None);
                        }

                        var posList = columns.Count > 2
                            ? ParseMultiplePartsOfSpeech(columns[2].Trim(), PartOfSpeech.Noun)
                            : new List<PartOfSpeech> { PartOfSpeech.Noun };
                        var primaryPos = posList.Count > 0 ? posList[0] : PartOfSpeech.Noun;

                        string? phoneticUk = columns.Count > 3 && !string.IsNullOrWhiteSpace(columns[3]) ? columns[3].Trim() : null;
                        string? phoneticUs = columns.Count > 4 && !string.IsNullOrWhiteSpace(columns[4]) ? columns[4].Trim() : null;
                        string meaningsVi = columns.Count > 5 ? columns[5].Trim() : term;
                        string examplesText = columns.Count > 6 ? columns[6].Trim() : "";
                        string topicsText = columns.Count > 7 ? columns[7].Trim() : "";
                        string examTagsText = columns.Count > 8 ? columns[8].Trim() : "";
                        string? notes = columns.Count > 9 ? columns[9].Trim() : null;

                        if (existingDict.TryGetValue(normalized, out var existing))
                        {
                            if (conflictResolution == ImportConflictResolution.Overwrite)
                            {
                                if (level != CefrLevel.None) existing.Level = level;
                                if (!string.IsNullOrWhiteSpace(phoneticUk)) existing.PhoneticUk = phoneticUk;
                                if (!string.IsNullOrWhiteSpace(phoneticUs)) existing.PhoneticUs = phoneticUs;
                                if (!string.IsNullOrWhiteSpace(notes)) existing.Notes = notes;
                                existing.PrimaryPartOfSpeech = primaryPos;
                                existing.UpdatedAt = DateTime.UtcNow;

                                _vocabRepo.RemoveWordChildren(existing);

                                var parsedMeanings = ParseMeaningsFromCsv(term, meaningsVi, posList, primaryPos, examplesText);
                                var newMeanings = new List<WordMeaning>();
                                int mIdx = 0;
                                foreach (var pm in parsedMeanings)
                                {
                                    var meaning = new WordMeaning
                                    {
                                        WordId = existing.Id,
                                        PartOfSpeech = pm.PartOfSpeech,
                                        DefinitionVi = pm.ResolvedDefinitionVi,
                                        DefinitionEn = pm.ResolvedDefinitionEn,
                                        Explanation = pm.Explanation,
                                        OrderIndex = mIdx++
                                    };

                                    foreach (var ex in pm.Examples)
                                    {
                                        if (!string.IsNullOrWhiteSpace(ex.ResolvedSentenceEn))
                                        {
                                            meaning.Examples.Add(new ExampleSentence
                                            {
                                                WordMeaningId = meaning.Id,
                                                SentenceEn = ex.ResolvedSentenceEn,
                                                SentenceVi = ex.ResolvedSentenceVi
                                            });
                                        }
                                    }
                                    newMeanings.Add(meaning);
                                    existing.Meanings.Add(meaning);
                                }
                                if (newMeanings.Count > 0)
                                {
                                    _vocabRepo.AddMeanings(newMeanings);
                                }

                                await _vocabRepo.UpdateAsync(existing, cancellationToken);
                                updated++;
                            }
                            else
                            {
                                skipped++;
                            }
                            continue;
                        }

                        var word = new Word
                        {
                            Term = term,
                            NormalizedTerm = normalized,
                            Level = level,
                            PrimaryPartOfSpeech = primaryPos,
                            PhoneticUk = phoneticUk,
                            PhoneticUs = phoneticUs,
                            Notes = notes
                        };

                        var generatedMeanings = ParseMeaningsFromCsv(term, meaningsVi, posList, primaryPos, examplesText);
                        foreach (var gm in generatedMeanings)
                        {
                            var meaning = new WordMeaning
                            {
                                WordId = word.Id,
                                PartOfSpeech = gm.PartOfSpeech,
                                DefinitionVi = gm.DefinitionVi,
                                DefinitionEn = gm.DefinitionEn,
                                Explanation = gm.Explanation,
                                OrderIndex = word.Meanings.Count
                            };
                            foreach (var ex in gm.Examples)
                            {
                                meaning.Examples.Add(new ExampleSentence
                                {
                                    WordMeaningId = meaning.Id,
                                    SentenceEn = ex.SentenceEn,
                                    SentenceVi = ex.SentenceVi
                                });
                            }
                            word.Meanings.Add(meaning);
                        }

                        if (!string.IsNullOrWhiteSpace(topicsText))
                        {
                            var topicTokens = topicsText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var tname in topicTokens)
                            {
                                if (allTopics.TryGetValue(tname.ToLowerInvariant(), out var topic))
                                {
                                    word.WordTopics.Add(new WordTopic { WordId = word.Id, TopicId = topic.Id });
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(examTagsText))
                        {
                            var tagTokens = examTagsText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var etname in tagTokens)
                            {
                                if (allExamTags.TryGetValue(etname.ToLowerInvariant(), out var examTag))
                                {
                                    word.WordExamTags.Add(new WordExamTag { WordId = word.Id, ExamTagId = examTag.Id });
                                }
                            }
                        }

                        word.ReviewCard = new ReviewCard
                        {
                            WordId = word.Id,
                            Status = LearningStatus.New,
                            NextReviewAt = DateTime.UtcNow
                        };

                        await _vocabRepo.AddAsync(word, cancellationToken);
                        imported++;
                    }

                    if (chunkStart + ImportBatchSize < dataRows.Count)
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        _unitOfWork.ClearTracker();
                    }

                    int pct = (int)((double)Math.Min(currentCount, total) / total * 75) + 15;
                    throttler.Report(pct, currentCount, total, $"Đang xử lý CSV: {currentCount} / {total}", chunk[^1].Count > 0 ? chunk[^1][0] : null);
                }

                throttler.Report(95, total, total, "Đang ghi dữ liệu vào cơ sở dữ liệu...", "Committing transaction...", force: true);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                _unitOfWork.ClearTracker();
                throttler.Report(100, total, total, "Nhập CSV hoàn tất!", "Complete.", force: true);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing CSV: {ex.Message}");
        }

        return new ImportResult(imported, skipped, errors, updated);
    }

    public async Task<ImportResult> ImportFromTxtAsync(
        string txtContent,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        int imported = 0;
        int updated = 0;
        int skipped = 0;
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(txtContent))
        {
            return new ImportResult(0, 0, new List<string> { "TXT content is empty" }, 0);
        }

        if (txtContent.Length > MaxImportPayloadLength)
        {
            return new ImportResult(0, 0, new List<string> { $"Payload exceeds maximum allowed size of {MaxImportPayloadLength / (1024 * 1024)} MB" }, 0);
        }

        var throttler = new ProgressThrottler(progress);

        try
        {
            throttler.Report(5, 0, 0, "Đang phân tích cấu trúc văn bản TXT...", "Parsing TXT lines...", force: true);
            using var reader = new StringReader(txtContent);
            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parsedRows = new List<(string Term, string Normalized, CefrLevel Level, PartOfSpeech Pos, string? Uk, string? Us, string Definition, string? ExamplesStr)>();
            string? line;
            int lineNumber = 0;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                lineNumber++;
                var trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#') || trimmed.StartsWith("//"))
                {
                    continue;
                }

                trimmed = trimmed.TrimStart('-', '*', '•', ' ').Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                string term = "";
                string definition = "";
                string? posStr = null;
                string? levelStr = null;
                string? phoneticUk = null;
                string? phoneticUs = null;
                string? examplesStr = null;

                if (trimmed.Contains('\t'))
                {
                    var parts = trimmed.Split('\t');
                    term = parts[0].Trim();
                    if (parts.Length > 1) definition = parts[1].Trim();
                    if (parts.Length > 2) posStr = parts[2].Trim();
                    if (parts.Length > 3) levelStr = parts[3].Trim();
                    if (parts.Length > 4) phoneticUk = parts[4].Trim();
                    if (parts.Length > 5) phoneticUs = parts[5].Trim();
                    if (parts.Length > 6) examplesStr = parts[6].Trim();
                }
                else
                {
                    string[] delimiters = { " -> ", " => ", " : ", " - ", " | ", " = " };
                    int foundDelimIdx = -1;
                    string matchedDelim = "";

                    foreach (var delim in delimiters)
                    {
                        var idx = trimmed.IndexOf(delim, StringComparison.Ordinal);
                        if (idx > 0 && (foundDelimIdx == -1 || idx < foundDelimIdx))
                        {
                            foundDelimIdx = idx;
                            matchedDelim = delim;
                        }
                    }

                    if (foundDelimIdx > 0)
                    {
                        term = trimmed.Substring(0, foundDelimIdx).Trim();
                        definition = trimmed.Substring(foundDelimIdx + matchedDelim.Length).Trim();
                    }
                    else if (trimmed.Contains(':'))
                    {
                        var idx = trimmed.IndexOf(':');
                        term = trimmed.Substring(0, idx).Trim();
                        definition = trimmed.Substring(idx + 1).Trim();
                    }
                    else if (trimmed.Contains('-'))
                    {
                        var idx = trimmed.IndexOf('-');
                        term = trimmed.Substring(0, idx).Trim();
                        definition = trimmed.Substring(idx + 1).Trim();
                    }
                    else
                    {
                        term = trimmed;
                        definition = trimmed;
                    }
                }

                if (term.StartsWith("**") && term.EndsWith("**") && term.Length > 4)
                {
                    term = term.Substring(2, term.Length - 4).Trim();
                }

                if (string.IsNullOrWhiteSpace(term))
                {
                    skipped++;
                    continue;
                }

                if (term.Contains('/') && phoneticUk == null)
                {
                    var slashStart = term.IndexOf('/');
                    var slashEnd = term.LastIndexOf('/');
                    if (slashEnd > slashStart)
                    {
                        phoneticUk = term.Substring(slashStart, slashEnd - slashStart + 1).Trim();
                        phoneticUs = phoneticUk;
                        term = term.Substring(0, slashStart).Trim().TrimEnd('(', '[').Trim();
                    }
                }

                if (posStr == null && term.EndsWith(')') && term.Contains('('))
                {
                    var openIdx = term.LastIndexOf('(');
                    var candidatePos = term.Substring(openIdx + 1, term.Length - openIdx - 2).Trim();
                    if (candidatePos.Length <= 4)
                    {
                        posStr = candidatePos;
                        term = term.Substring(0, openIdx).Trim();
                    }
                }

                var normalized = NormalizeTerm(term);

                if (!seenTerms.Add(normalized))
                {
                    skipped++;
                    continue;
                }

                var pos = ParsePartOfSpeech(posStr, PartOfSpeech.Noun);
                var level = ParseCefrLevel(levelStr, CefrLevel.None);

                parsedRows.Add((term, normalized, level, pos, phoneticUk, phoneticUs, definition, examplesStr));
            }

            int total = parsedRows.Count;
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                for (int chunkStart = 0; chunkStart < total; chunkStart += ImportBatchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var chunk = parsedRows.Skip(chunkStart).Take(ImportBatchSize).ToList();
                    var chunkTerms = chunk.Select(r => r.Normalized).Distinct().ToList();

                    IReadOnlyDictionary<string, Word>? existingDict = null;
                    try
                    {
                        existingDict = await _vocabRepo.GetWordsByTermsWithDetailsAsync(chunkTerms, cancellationToken);
                    }
                    catch
                    {
                        existingDict = null;
                    }

                    if (existingDict == null)
                    {
                        var fallbackDict = new Dictionary<string, Word>(StringComparer.OrdinalIgnoreCase);
                        foreach (var termKey in chunkTerms)
                        {
                            var w = await _vocabRepo.GetByTermAsync(termKey, cancellationToken);
                            if (w != null) fallbackDict[termKey] = w;
                        }
                        existingDict = fallbackDict;
                    }

                    int currentCount = chunkStart;
                    foreach (var row in chunk)
                    {
                        currentCount++;
                        if (existingDict.TryGetValue(row.Normalized, out var existing))
                        {
                            if (conflictResolution == ImportConflictResolution.Overwrite)
                            {
                                if (row.Level != CefrLevel.None) existing.Level = row.Level;
                                if (!string.IsNullOrWhiteSpace(row.Uk)) existing.PhoneticUk = row.Uk;
                                if (!string.IsNullOrWhiteSpace(row.Us)) existing.PhoneticUs = row.Us;
                                existing.PrimaryPartOfSpeech = row.Pos;
                                existing.UpdatedAt = DateTime.UtcNow;

                                _vocabRepo.RemoveWordChildren(existing);

                                if (!string.IsNullOrWhiteSpace(row.Definition))
                                {
                                    var meaning = new WordMeaning
                                    {
                                        WordId = existing.Id,
                                        PartOfSpeech = row.Pos,
                                        DefinitionVi = row.Definition,
                                        OrderIndex = 0
                                    };
                                    existing.Meanings.Add(meaning);
                                    _vocabRepo.AddMeanings(new[] { meaning });
                                }
                                await _vocabRepo.UpdateAsync(existing, cancellationToken);
                                updated++;
                            }
                            else
                            {
                                skipped++;
                            }
                            continue;
                        }

                        var word = new Word
                        {
                            Term = row.Term,
                            NormalizedTerm = row.Normalized,
                            Level = row.Level,
                            PrimaryPartOfSpeech = row.Pos,
                            PhoneticUk = !string.IsNullOrWhiteSpace(row.Uk) ? row.Uk : null,
                            PhoneticUs = !string.IsNullOrWhiteSpace(row.Us) ? row.Us : null
                        };

                        var primaryMeaning = new WordMeaning
                        {
                            WordId = word.Id,
                            PartOfSpeech = row.Pos,
                            DefinitionVi = !string.IsNullOrWhiteSpace(row.Definition) ? row.Definition : row.Term,
                            OrderIndex = 0
                        };

                        if (!string.IsNullOrWhiteSpace(row.ExamplesStr))
                        {
                            var exampleTokens = row.ExamplesStr.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var exToken in exampleTokens)
                            {
                                var arrowIdx = exToken.IndexOf("=>", StringComparison.Ordinal);
                                string en = arrowIdx >= 0 ? exToken.Substring(0, arrowIdx).Trim() : exToken.Trim();
                                string vi = arrowIdx >= 0 ? exToken.Substring(arrowIdx + 2).Trim() : "";
                                if (!string.IsNullOrWhiteSpace(en))
                                {
                                    primaryMeaning.Examples.Add(new ExampleSentence
                                    {
                                        WordMeaningId = primaryMeaning.Id,
                                        SentenceEn = en,
                                        SentenceVi = vi
                                    });
                                }
                            }
                        }

                        word.Meanings.Add(primaryMeaning);

                        word.ReviewCard = new ReviewCard
                        {
                            WordId = word.Id,
                            Status = LearningStatus.New,
                            NextReviewAt = DateTime.UtcNow
                        };

                        await _vocabRepo.AddAsync(word, cancellationToken);
                        imported++;
                    }

                    if (chunkStart + ImportBatchSize < total)
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        _unitOfWork.ClearTracker();
                    }

                    int pct = total > 0 ? (int)((double)Math.Min(currentCount, total) / total * 75) + 15 : 90;
                    throttler.Report(pct, currentCount, total, $"Đang xử lý TXT: {currentCount} / {total}", chunk[^1].Term);
                }

                if (imported > 0 || updated > 0)
                {
                    throttler.Report(95, imported + updated, total, "Đang lưu vào cơ sở dữ liệu...", "Committing transaction...", force: true);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    _unitOfWork.ClearTracker();
                }
                else
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                throttler.Report(100, imported + updated, total, "Nhập TXT hoàn tất!", "Complete.", force: true);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                throw;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error parsing TXT: {ex.Message}");
        }

        return new ImportResult(imported, skipped, errors, updated);
    }
}
