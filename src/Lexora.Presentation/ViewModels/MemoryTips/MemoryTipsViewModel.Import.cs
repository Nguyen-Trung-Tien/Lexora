#nullable enable
using System.Text.Json;
using Lexora.Application.DTOs.MemoryTips;
using Lexora.Domain.Enums;

namespace Lexora.Presentation.ViewModels.MemoryTips;

public partial class MemoryTipsViewModel
{
    public string GetSampleImportTemplate() =>
        """
        [
          {
            "title": "Adjective Order (OpSASCOMP)",
            "mnemonicHook": "Opinion - Size - Age - Shape - Color - Origin - Material - Purpose",
            "explanation": "Use OpSASCOMP to order multiple adjectives before a noun naturally.",
            "example": "A lovely small old round red French wooden jewelry box.",
            "commonTrap": "Putting material or origin before age or size.",
            "category": "Grammar",
            "technique": "Acrostic",
            "examTarget": "TOEIC",
            "tags": "adjective,order,grammar"
          }
        ]
        """;

    public async Task<(int Imported, int Skipped)> ImportTipsSmartAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content) || IsSubmitting)
        {
            return (0, 0);
        }

        IsSubmitting = true;
        try
        {
            var existingTips = await _tipService.GetTipsAsync(cancellationToken: cancellationToken);
            var existingTitles = existingTips
                .Select(t => t.Title.Trim().ToLowerInvariant())
                .ToHashSet();

            var candidates = ParseSmartImportContent(content);
            int imported = 0;
            int skipped = 0;

            foreach (var item in candidates)
            {
                var normalizedTitle = item.Title.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(normalizedTitle) || existingTitles.Contains(normalizedTitle))
                {
                    skipped++;
                    continue;
                }

                await _tipService.CreateAsync(item, cancellationToken);
                existingTitles.Add(normalizedTitle);
                imported++;
            }

            await LoadTipsAsync();
            StatusMessage = string.Format(
                _localizationService.GetString("MemoryTips_SmartImport_Summary"),
                imported,
                skipped);
            IsErrorStatus = imported == 0 && skipped > 0;
            return (imported, skipped);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            IsErrorStatus = true;
            return (0, 0);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private static List<CreateMemoryTipRequest> ParseSmartImportContent(string content)
    {
        var results = new List<CreateMemoryTipRequest>();
        var trimmed = content.Trim();

        if (trimmed.StartsWith('[') || trimmed.StartsWith('{'))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                if (trimmed.StartsWith('['))
                {
                    var items = JsonSerializer.Deserialize<List<SmartTipImportItem>>(trimmed, options);
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            if (!string.IsNullOrWhiteSpace(item.Title))
                            {
                                results.Add(ToCreateRequest(item));
                            }
                        }
                        return results;
                    }
                }
                else
                {
                    var single = JsonSerializer.Deserialize<SmartTipImportItem>(trimmed, options);
                    if (single != null && !string.IsNullOrWhiteSpace(single.Title))
                    {
                        results.Add(ToCreateRequest(single));
                        return results;
                    }
                }
            }
            catch (JsonException)
            {
                // Fall back to line-based parsing
            }
        }

        var lines = trimmed.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith('#') || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            char[] separators = line.Contains('\t') ? ['\t'] : line.Contains('|') ? ['|'] : [','];
            var parts = line.Split(separators, StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            // Skip CSV header row if detected
            if (parts[0].Equals("Title", StringComparison.OrdinalIgnoreCase) &&
                parts[1].Contains("Hook", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new CreateMemoryTipRequest
            {
                Title = parts[0],
                MnemonicHook = parts[1],
                Explanation = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : parts[1],
                Example = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3] : null,
                CommonTrap = parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]) ? parts[4] : null,
                Category = TipCategory.Vocabulary,
                Technique = MnemonicTechnique.Acrostic
            });
        }

        return results;
    }

    private static CreateMemoryTipRequest ToCreateRequest(SmartTipImportItem item)
    {
        _ = Enum.TryParse<TipCategory>(item.Category, true, out var category);
        _ = Enum.TryParse<MnemonicTechnique>(item.Technique, true, out var technique);
        ExamTarget? examTarget = Enum.TryParse<ExamTarget>(item.ExamTarget, true, out var parsedExam)
            ? parsedExam
            : null;

        return new CreateMemoryTipRequest
        {
            Title = item.Title.Trim(),
            MnemonicHook = string.IsNullOrWhiteSpace(item.MnemonicHook) ? item.Title.Trim() : item.MnemonicHook.Trim(),
            Explanation = string.IsNullOrWhiteSpace(item.Explanation) ? item.Title.Trim() : item.Explanation.Trim(),
            Example = string.IsNullOrWhiteSpace(item.Example) ? null : item.Example.Trim(),
            CommonTrap = string.IsNullOrWhiteSpace(item.CommonTrap) ? null : item.CommonTrap.Trim(),
            Category = category,
            Technique = technique,
            ExamTarget = examTarget,
            Tags = string.IsNullOrWhiteSpace(item.Tags) ? null : item.Tags.Trim()
        };
    }

    private sealed class SmartTipImportItem
    {
        public string Title { get; set; } = string.Empty;
        public string MnemonicHook { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? Example { get; set; }
        public string? CommonTrap { get; set; }
        public string? Category { get; set; }
        public string? Technique { get; set; }
        public string? ExamTarget { get; set; }
        public string? Tags { get; set; }
    }
}
