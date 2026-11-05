using System.Reflection;
using System.Text.RegularExpressions;
using Lexora.Application.Interfaces;

namespace Lexora.Infrastructure.Services;

/// <summary>
/// Infrastructure service for loading, parsing, and caching offline release notes logs from markdown files.
/// </summary>
public sealed class ReleaseNotesService : IReleaseNotesService
{
    private readonly string _releaseNotesDir;
    private readonly string _currentVersion;
    private readonly Lazy<Task<IReadOnlyList<ReleaseNoteItem>>> _cachedNotes;

    public ReleaseNotesService(string? customNotesDirectory = null, string? customAppVersion = null)
    {
        _currentVersion = customAppVersion ?? ResolveCurrentAppVersion();
        _releaseNotesDir = customNotesDirectory ?? ResolveReleaseNotesDirectory();
        _cachedNotes = new Lazy<Task<IReadOnlyList<ReleaseNoteItem>>>(LoadNotesInternalAsync);
    }

    public async Task<IReadOnlyList<ReleaseNoteItem>> GetAllReleaseNotesAsync(CancellationToken cancellationToken = default)
    {
        return await _cachedNotes.Value;
    }

    public async Task<ReleaseNoteItem?> GetReleaseNoteAsync(string version, CancellationToken cancellationToken = default)
    {
        var all = await GetAllReleaseNotesAsync(cancellationToken);
        var norm = NormalizeVersion(version);
        return all.FirstOrDefault(n => NormalizeVersion(n.Version) == norm);
    }

    public async Task<ReleaseNoteItem?> GetCurrentVersionReleaseNoteAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllReleaseNotesAsync(cancellationToken);
        return all.FirstOrDefault(n => n.IsCurrent) ?? all.FirstOrDefault();
    }

    private async Task<IReadOnlyList<ReleaseNoteItem>> LoadNotesInternalAsync()
    {
        var items = new List<ReleaseNoteItem>();

        if (Directory.Exists(_releaseNotesDir))
        {
            var files = Directory.GetFiles(_releaseNotesDir, "v*.md");
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath); // e.g., "v1.6.1"
                var rawVersion = fileName.TrimStart('v', 'V');

                try
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    var item = ParseMarkdownFile(rawVersion, content, _currentVersion);
                    items.Add(item);
                }
                catch
                {
                    // Ignore transient file read issues
                }
            }
        }

        if (items.Count == 0)
        {
            // Built-in fallback in case markdown directory was not bundled in the execution root
            items.AddRange(GetFallbackReleaseNotes(_currentVersion));
        }

        // Order descending by Semantic Version
        return items
            .OrderByDescending(i => ParseSemVer(i.Version))
            .ToList()
            .AsReadOnly();
    }

    private static ReleaseNoteItem ParseMarkdownFile(string version, string markdown, string currentVersion)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        string title = $"Lexora v{version}";
        string releaseDate = DateTime.Now.ToString("yyyy-MM-dd");
        string summary = string.Empty;

        // Parse title from '# Title'
        var titleLine = lines.FirstOrDefault(l => l.StartsWith('#'));
        if (!string.IsNullOrWhiteSpace(titleLine))
        {
            title = titleLine.TrimStart('#').Trim();
        }

        // Parse date from '> Ngày phát hành: YYYY-MM-DD'
        var dateMatch = Regex.Match(markdown, @"(\d{4}-\d{2}-\d{2})");
        if (dateMatch.Success)
        {
            releaseDate = dateMatch.Groups[1].Value;
        }

        // Parse summary from highlights section
        int highlightIdx = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("Điểm nổi bật") || lines[i].Contains("Highlights"))
            {
                highlightIdx = i;
                break;
            }
        }

        if (highlightIdx >= 0 && highlightIdx + 1 < lines.Length)
        {
            for (int i = highlightIdx + 1; i < lines.Length && i < highlightIdx + 5; i++)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith('#') && !line.StartsWith("---"))
                {
                    summary = line;
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            summary = $"Lexora v{version} release featuring new capabilities and learning improvements.";
        }

        bool isCurrent = NormalizeVersion(version) == NormalizeVersion(currentVersion);

        return new ReleaseNoteItem(
            Version: version,
            Title: title,
            ReleaseDate: releaseDate,
            Summary: summary,
            MarkdownContent: markdown,
            IsCurrent: isCurrent);
    }

    private static string ResolveCurrentAppVersion()
    {
        try
        {
            var entry = Assembly.GetEntryAssembly();
            var name = entry?.GetName().Name;
            if (entry != null &&
                !string.IsNullOrEmpty(name) &&
                !name.StartsWith("testhost", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("vstest", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase))
            {
                var ver = entry.GetName().Version;
                if (ver != null && ver.Major > 0)
                {
                    return $"{ver.Major}.{ver.Minor}.{ver.Build}";
                }
            }
        }
        catch
        {
            // Fallback
        }

        return "1.6.1";
    }

    private static string ResolveReleaseNotesDirectory()
    {
        // 1. App output / installation folder
        var localDir = Path.Combine(AppContext.BaseDirectory, "release-notes");
        if (Directory.Exists(localDir))
        {
            return localDir;
        }

        // 2. Walk up to locate workspace root (for tests and development debugging)
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && current != null; i++)
        {
            var candidate = Path.Combine(current.FullName, "release-notes");
            if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "v*.md").Length > 0)
            {
                return candidate;
            }
            current = current.Parent;
        }

        return localDir;
    }

    private static Version ParseSemVer(string version)
    {
        var cleaned = NormalizeVersion(version);
        if (Version.TryParse(cleaned, out var parsed))
        {
            return parsed;
        }

        return new Version(0, 0, 0);
    }

    private static string NormalizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return "0.0.0";
        var cleaned = version.Trim().TrimStart('v', 'V');
        var dashIdx = cleaned.IndexOf('-');
        if (dashIdx >= 0) cleaned = cleaned[..dashIdx];
        var plusIdx = cleaned.IndexOf('+');
        if (plusIdx >= 0) cleaned = cleaned[..plusIdx];

        var parts = cleaned.Split('.');
        if (parts.Length == 1) return $"{parts[0]}.0.0";
        if (parts.Length == 2) return $"{parts[0]}.{parts[1]}.0";
        if (parts.Length >= 3) return $"{parts[0]}.{parts[1]}.{parts[2]}";
        return cleaned;
    }

    private static IReadOnlyList<ReleaseNoteItem> GetFallbackReleaseNotes(string currentVersion)
    {
        return new List<ReleaseNoteItem>
        {
            new(
                Version: "1.6.1",
                Title: "Lexora v1.6.1 — Interactive Fluent 2 Roadmap & System Polish",
                ReleaseDate: "2026-11-05",
                Summary: "Lexora v1.6.1 introduces a redesigned Fluent 2 Interactive Learning Pathway, next-generation FSRS v5 spaced repetition, and multi-word homophone comparison.",
                MarkdownContent: "# Lexora v1.6.1 — Interactive Fluent 2 Roadmap & System Polish\n> Release Date: 2026-11-05 | Version: 1.6.1\n\n## 🌟 Highlights\nLexora v1.6.1 introduces a redesigned **Fluent 2 Interactive Learning Pathway**, next-generation **FSRS v5 spaced repetition**, dynamic multi-word homophone comparison, and a 95% reduction in memory footprint.\n\n## 🚀 New Features\n- **Interactive Fluent 2 Learning Pathway**: Visual milestone tracks for TOEIC, IELTS, and VSTEP preparation with progressive lesson unlocking.\n- **FSRS v5 Spaced Repetition**: Memory stability and difficulty metrics for personalized vocabulary retention schedules.\n- **Multi-Word Homophone Sets**: Compare 3+ identically sounding words simultaneously (e.g., there / their / they're).\n- **Smart Adaptive Quiz Navigation**: Seamless transition to 'Finish Test' on the final quiz question.",
                IsCurrent: NormalizeVersion("1.6.1") == NormalizeVersion(currentVersion)),

            new(
                Version: "1.6.0",
                Title: "Lexora v1.6.0 — Adaptive Roadmap & Target Ecosystem",
                ReleaseDate: "2026-11-05",
                Summary: "Version 1.6.0 unveils the Adaptive Learning Roadmap module and introduces comprehensive system-wide keyboard shortcuts.",
                MarkdownContent: "# Lexora v1.6.0 — Adaptive Roadmap & Target Ecosystem\n> Release Date: 2026-11-05 | Version: 1.6.0\n\n## 🌟 Highlights\nVersion 1.6.0 unveils the **Adaptive Learning Roadmap** module and introduces comprehensive system-wide keyboard shortcuts for effortless navigation.\n\n## 🚀 New Features\n- **Adaptive Roadmap Engine**: Tailored study pathways matching user proficiency gaps.\n- **Global Keyboard Shortcuts (Ctrl + 1 to Ctrl + 9)**: Instant keyboard switching across study modules.",
                IsCurrent: NormalizeVersion("1.6.0") == NormalizeVersion(currentVersion))
        };
    }
}

