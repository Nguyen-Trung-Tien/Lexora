using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.Services;

public class CommunityDataService : ICommunityDataService
{
    private const int MaxPackageSizeBytes = 20 * 1024 * 1024; // 20 MB safety ceiling
    private const string DefaultRepoOwner = "Nguyen-Trung-Tien";
    private const string DefaultRepoName = "Lexora";
    private const string DefaultBranch = "main";
    private const string DefaultCatalogPath = "community/catalog.json";

    private readonly IExportImportService _exportImportService;
    private readonly IGrammarExportImportService _grammarExportImportService;
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly ILogger<CommunityDataService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _installedPackagesFilePath;
    private readonly string _localCatalogCachePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CommunityDataService(
        IExportImportService exportImportService,
        IGrammarExportImportService grammarExportImportService,
        IUserPreferencesService userPreferencesService,
        ILogger<CommunityDataService> logger,
        HttpClient? httpClient = null)
    {
        _exportImportService = exportImportService;
        _grammarExportImportService = grammarExportImportService;
        _userPreferencesService = userPreferencesService;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDir = Path.Combine(appData, "Lexora", "data");
        Directory.CreateDirectory(dataDir);
        _installedPackagesFilePath = Path.Combine(dataDir, "installed_packages.json");
        _localCatalogCachePath = Path.Combine(dataDir, "catalog_cache.json");
    }

    /// <inheritdoc/>
    public async Task<CommunityCatalogDto> GetCatalogAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var prefs = _userPreferencesService.GetPreferences();
        var catalogUrl = !string.IsNullOrWhiteSpace(prefs.CommunityCatalogUrl)
            ? prefs.CommunityCatalogUrl.Trim()
            : $"https://raw.githubusercontent.com/{DefaultRepoOwner}/{DefaultRepoName}/{DefaultBranch}/{DefaultCatalogPath}";

        if (!forceRefresh && File.Exists(_localCatalogCachePath))
        {
            try
            {
                var cachedJson = await File.ReadAllTextAsync(_localCatalogCachePath, cancellationToken);
                var cached = JsonSerializer.Deserialize<CommunityCatalogDto>(cachedJson, JsonOptions);
                if (cached != null && cached.Packages.Count > 0)
                {
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed reading catalog cache. Will fetch fresh catalog.");
            }
        }

        // Try remote fetch
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, catalogUrl);
            request.Headers.UserAgent.ParseAdd("Lexora-Desktop-Client/1.2.1");
            if (!string.IsNullOrWhiteSpace(prefs.GitHubPersonalAccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", prefs.GitHubPersonalAccessToken.Trim());
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var remoteJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var remoteCatalog = JsonSerializer.Deserialize<CommunityCatalogDto>(remoteJson, JsonOptions);
                if (remoteCatalog != null && remoteCatalog.Packages.Count > 0)
                {
                    await File.WriteAllTextAsync(_localCatalogCachePath, remoteJson, cancellationToken);
                    return remoteCatalog;
                }
            }
            else
            {
                _logger.LogWarning("Remote catalog fetch returned HTTP {StatusCode}. Using fallback catalog.", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Remote catalog fetch failed. Falling back to local offline catalog.");
        }

        // Fallback to local catalog file if present in repository or app directory
        var localFallback = await LoadLocalFallbackCatalogAsync(cancellationToken);
        if (localFallback != null)
        {
            return localFallback;
        }

        return new CommunityCatalogDto
        {
            CatalogVersion = "1.2.1",
            Repository = $"{DefaultRepoOwner}/{DefaultRepoName}",
            LastUpdated = DateTime.UtcNow,
            Packages = []
        };
    }

    private async Task<CommunityCatalogDto?> LoadLocalFallbackCatalogAsync(CancellationToken cancellationToken)
    {
        var startDirs = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
        foreach (var start in startDirs)
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "community", "catalog.json");
                if (File.Exists(candidate))
                {
                    try
                    {
                        var text = await File.ReadAllTextAsync(candidate, cancellationToken);
                        var catalog = JsonSerializer.Deserialize<CommunityCatalogDto>(text, JsonOptions);
                        if (catalog != null && catalog.Packages.Count > 0)
                        {
                            return catalog;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed reading catalog from {Path}", candidate);
                    }
                }
                dir = dir.Parent;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CommunityPackageMetadataDto>> GetAvailablePackagesAsync(
        CommunityPackageFilterDto? filter = null,
        CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(forceRefresh: false, cancellationToken);
        var query = catalog.Packages.AsEnumerable();

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Category) && !filter.Category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => string.Equals(p.Category, filter.Category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.Level) && !filter.Level.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => string.Equals(p.Level, filter.Level, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.Exam) && !filter.Exam.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => string.Equals(p.TargetExam, filter.Exam, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var term = filter.SearchQuery.Trim();
                query = query.Where(p =>
                    p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }
        }

        return query.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<InstalledPackageRecord>> GetInstalledPackagesAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_installedPackagesFilePath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(_installedPackagesFilePath, cancellationToken);
            var list = JsonSerializer.Deserialize<List<InstalledPackageRecord>>(json, JsonOptions);
            return list ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read installed packages tracking file.");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<InstalledPackageRecord?> GetInstalledRecordAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var all = await GetInstalledPackagesAsync(cancellationToken);
        return all.FirstOrDefault(p => string.Equals(p.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task<CommunityPackagePayloadDto> DownloadPackageAsync(
        CommunityPackageMetadataDto package,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        progress?.Report(new ProgressReport(5, 0, 100, "Chuẩn bị tải gói dữ liệu...", "Preparing download..."));

        string rawContent;

        // If downloadUrl is relative or a local path, try local candidates first
        if (!package.DownloadUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !package.DownloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            string? localFound = null;
            var startDirs = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };
            foreach (var start in startDirs)
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    var candidate = Path.Combine(dir.FullName, package.DownloadUrl);
                    if (File.Exists(candidate))
                    {
                        localFound = candidate;
                        break;
                    }
                    dir = dir.Parent;
                }
                if (localFound != null) break;
            }

            if (localFound != null)
            {
                progress?.Report(new ProgressReport(50, 50, 100, "Đọc gói dữ liệu từ lưu trữ cục bộ...", "Reading local package..."));
                rawContent = await File.ReadAllTextAsync(localFound, cancellationToken);
            }
            else
            {
                // Convert relative path to remote GitHub URL
                var prefs = _userPreferencesService.GetPreferences();
                var resolvedRemoteUrl = $"https://raw.githubusercontent.com/{DefaultRepoOwner}/{DefaultRepoName}/{DefaultBranch}/{package.DownloadUrl}";
                rawContent = await DownloadFromUrlInternalAsync(resolvedRemoteUrl, prefs.GitHubPersonalAccessToken, progress, cancellationToken);
            }
        }
        else
        {
            var prefs = _userPreferencesService.GetPreferences();
            rawContent = await DownloadFromUrlInternalAsync(package.DownloadUrl, prefs.GitHubPersonalAccessToken, progress, cancellationToken);
        }

        // Checksum verification
        if (!string.IsNullOrWhiteSpace(package.ChecksumSha256))
        {
            progress?.Report(new ProgressReport(90, 90, 100, "Xác minh chữ ký số SHA256...", "Verifying SHA256 checksum..."));
            var bytes = Encoding.UTF8.GetBytes(rawContent);
            var computedHash = Convert.ToHexString(SHA256.HashData(bytes));
            if (!computedHash.Equals(package.ChecksumSha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Tính toàn vẹn của tệp tải về không hợp lệ. Checksum SHA256 không khớp! (Expected: {package.ChecksumSha256}, Got: {computedHash})");
            }
        }

        progress?.Report(new ProgressReport(95, 95, 100, "Kiểm tra cấu trúc và tính hợp lệ...", "Validating schema..."));
        return await ParseAndValidatePackageAsync(rawContent, cancellationToken);
    }

    private async Task<string> DownloadFromUrlInternalAsync(
        string url,
        string? patToken,
        IProgress<ProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Địa chỉ URL không hợp lệ: {url}");
        }

        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ cho phép tải qua giao thức bảo mật HTTPS.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("Lexora-Desktop-Client/1.2.1");
        if (!string.IsNullOrWhiteSpace(patToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", patToken.Trim());
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        if (totalBytes > MaxPackageSizeBytes)
        {
            throw new InvalidDataException($"Kích thước gói dữ liệu ({totalBytes / 1_048_576} MB) vượt quá giới hạn an toàn 20MB.");
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memoryStream = new MemoryStream();
        var buffer = new byte[8192];
        long readBytes = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            readBytes += bytesRead;
            if (readBytes > MaxPackageSizeBytes)
            {
                throw new InvalidDataException("Kích thước tệp tải về vượt quá giới hạn an toàn 20MB.");
            }

            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

            if (totalBytes > 0 && progress != null)
            {
                var percent = (int)((readBytes * 100) / totalBytes);
                progress.Report(new ProgressReport(percent, (int)readBytes, (int)totalBytes, $"Đang tải xuống ({percent}%)...", "Downloading package..."));
            }
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public async Task<CommunityPackagePayloadDto> ParseAndValidatePackageAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            throw new InvalidDataException("Nội dung gói dữ liệu trống.");
        }

        if (jsonContent.Length > MaxPackageSizeBytes)
        {
            throw new InvalidDataException("Dung lượng tệp vượt quá hạn mức tối đa 20MB.");
        }

        CommunityPackagePayloadDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<CommunityPackagePayloadDto>(jsonContent, JsonOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Định dạng JSON không hợp lệ hoặc bị lỗi cấu trúc: {ex.Message}");
        }

        if (payload == null || payload.Metadata == null)
        {
            throw new InvalidDataException("Thiếu phần thông tin metadata bắt buộc trong gói dữ liệu.");
        }

        if (string.IsNullOrWhiteSpace(payload.Metadata.Id))
        {
            throw new InvalidDataException("Metadata thiếu định danh 'Id' của gói dữ liệu.");
        }

        if (string.IsNullOrWhiteSpace(payload.Metadata.Name))
        {
            throw new InvalidDataException("Metadata thiếu tên gói dữ liệu ('Name').");
        }

        var isVocab = string.Equals(payload.Metadata.Category, "Vocabulary", StringComparison.OrdinalIgnoreCase);
        var isGrammar = string.Equals(payload.Metadata.Category, "Grammar", StringComparison.OrdinalIgnoreCase);

        if (!isVocab && !isGrammar)
        {
            throw new InvalidDataException($"Danh mục không được hỗ trợ: '{payload.Metadata.Category}'. Chỉ hỗ trợ 'Vocabulary' hoặc 'Grammar'.");
        }

        if (isVocab && (payload.VocabularyWords == null || payload.VocabularyWords.Count == 0))
        {
            throw new InvalidDataException("Gói từ vựng không chứa bất kỳ từ vựng nào hợp lệ.");
        }

        if (isGrammar && (payload.GrammarRules == null || payload.GrammarRules.Count == 0))
        {
            throw new InvalidDataException("Gói ngữ pháp không chứa bất kỳ bài ngữ pháp nào hợp lệ.");
        }

        return await Task.FromResult(payload);
    }

    /// <inheritdoc/>
    public async Task<ImportResult> InstallPackageAsync(
        CommunityPackagePayloadDto packagePayload,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packagePayload);

        var isVocab = string.Equals(packagePayload.Metadata.Category, "Vocabulary", StringComparison.OrdinalIgnoreCase);
        ImportResult result;

        if (isVocab)
        {
            var wordsJson = JsonSerializer.Serialize(packagePayload.VocabularyWords, JsonOptions);
            result = await _exportImportService.ImportFromJsonAsync(wordsJson, conflictResolution, cancellationToken, progress);
        }
        else
        {
            var rulesJson = JsonSerializer.Serialize(packagePayload.GrammarRules, JsonOptions);
            result = await _grammarExportImportService.ImportFromJsonAsync(rulesJson, conflictResolution, cancellationToken, progress);
        }

        result ??= new ImportResult(0, 0, []);

        if (result.TotalImported > 0 || result.TotalUpdated > 0)
        {
            await RecordInstalledPackageAsync(new InstalledPackageRecord
            {
                PackageId = packagePayload.Metadata.Id,
                Version = packagePayload.Metadata.Version,
                InstalledAt = DateTime.UtcNow,
                Name = packagePayload.Metadata.Name,
                Category = packagePayload.Metadata.Category,
                ItemCount = result.TotalImported + result.TotalUpdated,
                LocalSource = "Community"
            }, cancellationToken);
        }

        return result;
    }

    private async Task RecordInstalledPackageAsync(InstalledPackageRecord record, CancellationToken cancellationToken)
    {
        try
        {
            var list = (await GetInstalledPackagesAsync(cancellationToken)).ToList();
            list.RemoveAll(p => string.Equals(p.PackageId, record.PackageId, StringComparison.OrdinalIgnoreCase));
            list.Add(record);

            var json = JsonSerializer.Serialize(list, JsonOptions);
            await File.WriteAllTextAsync(_installedPackagesFilePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record installed package {Id}", record.PackageId);
        }
    }

    /// <inheritdoc/>
    public async Task<string> ExportPackageToJsonAsync(
        CommunityPackageMetadataDto metadata,
        IEnumerable<Guid>? selectedIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var isVocab = string.Equals(metadata.Category, "Vocabulary", StringComparison.OrdinalIgnoreCase);
        CommunityPackagePayloadDto payload;

        if (isVocab)
        {
            var wordsJson = await _exportImportService.ExportToJsonAsync(selectedIds, cancellationToken);
            var words = JsonSerializer.Deserialize<List<WordExportModel>>(wordsJson, JsonOptions) ?? [];
            var updatedMetadata = metadata with
            {
                ItemCount = words.Count,
                UpdatedAt = DateTime.UtcNow
            };

            payload = new CommunityPackagePayloadDto
            {
                FormatVersion = "1.0",
                Metadata = updatedMetadata,
                VocabularyWords = words,
                GrammarRules = null
            };
        }
        else
        {
            var rulesJson = await _grammarExportImportService.ExportToJsonAsync(selectedIds, cancellationToken);
            var rules = JsonSerializer.Deserialize<List<GrammarRuleExportModel>>(rulesJson, JsonOptions) ?? [];
            var updatedMetadata = metadata with
            {
                ItemCount = rules.Count,
                UpdatedAt = DateTime.UtcNow
            };

            payload = new CommunityPackagePayloadDto
            {
                FormatVersion = "1.0",
                Metadata = updatedMetadata,
                VocabularyWords = null,
                GrammarRules = rules
            };
        }

        var serialized = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(serialized);
        var checksum = Convert.ToHexString(SHA256.HashData(bytes));

        // Re-embed exact checksum and size
        var finalMetadata = payload.Metadata with
        {
            ChecksumSha256 = checksum,
            SizeInBytes = bytes.Length
        };

        var finalPayload = payload with { Metadata = finalMetadata };
        return JsonSerializer.Serialize(finalPayload, JsonOptions);
    }

    /// <inheritdoc/>
    public async Task<ImportResult> ImportPackageFromFileOrUrlAsync(
        string contentOrUrl,
        ImportConflictResolution conflictResolution = ImportConflictResolution.Skip,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentOrUrl))
        {
            return new ImportResult(0, 0, ["Nội dung hoặc đường dẫn URL trống."], 0);
        }

        string jsonContent;
        if (contentOrUrl.Trim().StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            contentOrUrl.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var prefs = _userPreferencesService.GetPreferences();
            jsonContent = await DownloadFromUrlInternalAsync(contentOrUrl.Trim(), prefs.GitHubPersonalAccessToken, progress, cancellationToken);
        }
        else
        {
            jsonContent = contentOrUrl.Trim();
        }

        var payload = await ParseAndValidatePackageAsync(jsonContent, cancellationToken);
        return await InstallPackageAsync(payload, conflictResolution, progress, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteInstalledRecordAsync(string packageId, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_installedPackagesFilePath))
        {
            return false;
        }

        try
        {
            var list = (await GetInstalledPackagesAsync(cancellationToken)).ToList();
            var removed = list.RemoveAll(p => string.Equals(p.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                var json = JsonSerializer.Serialize(list, JsonOptions);
                await File.WriteAllTextAsync(_installedPackagesFilePath, json, cancellationToken);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete installed package record {Id}", packageId);
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> CheckPackageUpdateAsync(string packageId, string currentVersion, CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(forceRefresh: true, cancellationToken);
        var package = catalog.Packages.FirstOrDefault(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));
        if (package == null)
        {
            return false;
        }

        if (Version.TryParse(package.Version.TrimStart('v', 'V'), out var remoteVer) &&
            Version.TryParse(currentVersion.TrimStart('v', 'V'), out var localVer))
        {
            return remoteVer > localVer;
        }

        return !string.Equals(package.Version, currentVersion, StringComparison.OrdinalIgnoreCase);
    }
}
