using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Presentation.Services;
using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.ViewModels;

public partial class CommunityPackageItemViewModel : ObservableObject
{
    private readonly CommunityPackageMetadataDto _dto;

    public CommunityPackageItemViewModel(CommunityPackageMetadataDto dto, CommunityPackageStatus status = CommunityPackageStatus.NotInstalled)
    {
        _dto = dto;
        Status = status;
    }

    public string Id => _dto.Id;
    public string Name => _dto.Name;
    public string Description => _dto.Description;
    public string Author => _dto.Author;
    public string Category => _dto.Category;
    public string TargetExam => _dto.TargetExam;
    public string Level => _dto.Level;
    public string Version => _dto.Version;
    public int ItemCount => _dto.ItemCount;
    public string Language => _dto.Language;
    public IReadOnlyList<string> Tags => _dto.Tags;
    public string DownloadUrl => _dto.DownloadUrl;
    public string? ChecksumSha256 => _dto.ChecksumSha256;
    public string License => _dto.License;
    public long SizeInBytes => _dto.SizeInBytes;
    public DateTime UpdatedAt => _dto.UpdatedAt;
    public CommunityPackageMetadataDto Dto => _dto;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInstall))]
    [NotifyPropertyChangedFor(nameof(IsInstalled))]
    [NotifyPropertyChangedFor(nameof(HasUpdate))]
    [NotifyPropertyChangedFor(nameof(IsWorking))]
    public partial CommunityPackageStatus Status { get; set; } = CommunityPackageStatus.NotInstalled;

    [ObservableProperty]
    public partial int DownloadProgress { get; set; } = 0;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public bool CanInstall => Status == CommunityPackageStatus.NotInstalled || Status == CommunityPackageStatus.Error;
    public bool IsInstalled => Status == CommunityPackageStatus.Installed;
    public bool HasUpdate => Status == CommunityPackageStatus.UpdateAvailable;
    public bool IsWorking => Status == CommunityPackageStatus.Downloading || Status == CommunityPackageStatus.Installing;

    public string FormattedSize
    {
        get
        {
            if (SizeInBytes <= 0) return "N/A";
            if (SizeInBytes < 1024) return $"{SizeInBytes} B";
            if (SizeInBytes < 1024 * 1024) return $"{(SizeInBytes / 1024.0):F1} KB";
            return $"{(SizeInBytes / (1024.0 * 1024.0)):F1} MB";
        }
    }

    public string TagsDisplay => Tags.Count > 0 ? string.Join(", ", Tags.Select(t => $"#{t}")) : string.Empty;
}

public partial class DownloadCenterViewModel : ObservableObject, IDisposable
{
    private readonly ICommunityDataService _communityDataService;
    private readonly ILocalizationService _localizationService;
    private readonly IVocabularyService _vocabularyService;
    private readonly IGrammarService _grammarService;
    private readonly IToastNotificationService _toastService;
    private readonly ISoundFeedbackService _soundService;
    private readonly DispatcherQueue _dispatcherQueue;

    public ILocalizationService Loc => _localizationService;

    // Tabs: 0 = Vocabulary, 1 = Grammar, 2 = My Downloads, 3 = Import & Share
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVocabTab))]
    [NotifyPropertyChangedFor(nameof(IsGrammarTab))]
    [NotifyPropertyChangedFor(nameof(IsMyDownloadsTab))]
    [NotifyPropertyChangedFor(nameof(IsImportShareTab))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial int SelectedTab { get; set; } = 0;

    public bool IsVocabTab => SelectedTab == 0;
    public bool IsGrammarTab => SelectedTab == 1;
    public bool IsMyDownloadsTab => SelectedTab == 2;
    public bool IsImportShareTab => SelectedTab == 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial bool IsOfflineMode { get; set; } = false;

    [ObservableProperty]
    public partial bool HasError { get; set; } = false;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedLevelFilter { get; set; } = "All";

    [ObservableProperty]
    public partial string SelectedExamFilter { get; set; } = "All";

    public ObservableCollection<CommunityPackageItemViewModel> Packages { get; } = [];
    public ObservableCollection<InstalledPackageRecord> InstalledPackages { get; } = [];

    public bool IsEmpty => !IsLoading &&
        ((IsVocabTab || IsGrammarTab) ? Packages.Count == 0 :
         IsMyDownloadsTab ? InstalledPackages.Count == 0 : false);

    // Export Package Form Properties
    [ObservableProperty]
    public partial string ExportName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExportDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExportAuthor { get; set; } = "Lexora User";

    [ObservableProperty]
    public partial string ExportCategory { get; set; } = "Vocabulary";

    [ObservableProperty]
    public partial string ExportExam { get; set; } = "General";

    [ObservableProperty]
    public partial string ExportLevel { get; set; } = "B1";

    [ObservableProperty]
    public partial string ExportTags { get; set; } = "Community";

    [ObservableProperty]
    public partial bool IsExporting { get; set; } = false;

    // Direct URL Import Form Properties
    [ObservableProperty]
    public partial string DirectUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedConflictResolutionIndex { get; set; } = 0; // 0 = Skip, 1 = Overwrite

    [ObservableProperty]
    public partial bool IsImportingUrl { get; set; } = false;

    [ObservableProperty]
    public partial int ImportUrlProgress { get; set; } = 0;

    [ObservableProperty]
    public partial string ImportUrlStatus { get; set; } = string.Empty;

    public DownloadCenterViewModel(
        ICommunityDataService communityDataService,
        ILocalizationService localizationService,
        IVocabularyService vocabularyService,
        IGrammarService grammarService,
        IToastNotificationService toastService,
        ISoundFeedbackService soundService)
    {
        _communityDataService = communityDataService;
        _localizationService = localizationService;
        _vocabularyService = vocabularyService;
        _grammarService = grammarService;
        _toastService = toastService;
        _soundService = soundService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _localizationService.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Loc));
    }

    public async Task InitializeAsync()
    {
        await LoadCatalogAsync(forceRefresh: false);
    }

    [RelayCommand]
    public async Task RefreshCatalogAsync()
    {
        await LoadCatalogAsync(forceRefresh: true);
    }

    public async Task LoadCatalogAsync(bool forceRefresh = false)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var category = IsVocabTab ? "Vocabulary" :
                           IsGrammarTab ? "Grammar" : null;

            var filter = new CommunityPackageFilterDto(
                Category: category,
                SearchQuery: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                Level: SelectedLevelFilter,
                Exam: SelectedExamFilter);

            var rawCatalog = await _communityDataService.GetCatalogAsync(forceRefresh);
            IsOfflineMode = string.IsNullOrWhiteSpace(rawCatalog.Repository);

            var available = await _communityDataService.GetAvailablePackagesAsync(filter);
            var installed = await _communityDataService.GetInstalledPackagesAsync();

            var installedDict = installed.ToDictionary(p => p.PackageId, StringComparer.OrdinalIgnoreCase);

            Packages.Clear();
            foreach (var pkg in available)
            {
                var status = CommunityPackageStatus.NotInstalled;
                if (installedDict.TryGetValue(pkg.Id, out var inst))
                {
                    if (Version.TryParse(pkg.Version.TrimStart('v', 'V'), out var remoteV) &&
                        Version.TryParse(inst.Version.TrimStart('v', 'V'), out var localV) &&
                        remoteV > localV)
                    {
                        status = CommunityPackageStatus.UpdateAvailable;
                    }
                    else
                    {
                        status = CommunityPackageStatus.Installed;
                    }
                }

                Packages.Add(new CommunityPackageItemViewModel(pkg, status));
            }

            InstalledPackages.Clear();
            foreach (var inst in installed)
            {
                InstalledPackages.Add(inst);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    public async Task DownloadAndInstallPackageAsync(CommunityPackageItemViewModel item)
    {
        if (item.IsWorking) return;

        item.Status = CommunityPackageStatus.Downloading;
        item.DownloadProgress = 0;
        item.StatusMessage = Loc.GetString("DownloadCenter_Downloading");

        var progress = new Progress<ProgressReport>(report =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                item.DownloadProgress = report.Percent;
                item.StatusMessage = report.Message;
            });
        });

        try
        {
            var payload = await _communityDataService.DownloadPackageAsync(item.Dto, progress);

            item.Status = CommunityPackageStatus.Installing;
            item.StatusMessage = Loc.GetString("DownloadCenter_Installing");

            var resolution = SelectedConflictResolutionIndex == 1
                ? ImportConflictResolution.Overwrite
                : ImportConflictResolution.Skip;

            var result = await _communityDataService.InstallPackageAsync(payload, resolution, progress);

            item.Status = CommunityPackageStatus.Installed;
            item.StatusMessage = Loc.GetString("DownloadCenter_Installed");

            _soundService.PlaySuccessSound();
            var successMsg = string.Format(
                Loc.GetString("DownloadCenter_InstallSuccess"),
                result.TotalImported,
                result.TotalUpdated,
                result.TotalSkipped);

            _toastService.ShowSuccess(item.Name, successMsg);

            // Refresh installed list
            await LoadCatalogAsync(forceRefresh: false);
        }
        catch (Exception ex)
        {
            item.Status = CommunityPackageStatus.Error;
            item.StatusMessage = ex.Message;
            _soundService.PlayErrorSound();
            _toastService.ShowError(Loc.GetString("Toast_ErrorTitle"), ex.Message);
        }
    }

    [RelayCommand]
    public async Task RemoveInstalledRecordAsync(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId)) return;

        var removed = await _communityDataService.DeleteInstalledRecordAsync(packageId);
        if (removed)
        {
            _soundService.PlaySuccessSound();
            await LoadCatalogAsync(forceRefresh: false);
        }
    }

    [RelayCommand]
    public async Task ImportFromDirectUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(DirectUrl)) return;

        IsImportingUrl = true;
        ImportUrlProgress = 0;
        ImportUrlStatus = Loc.GetString("DownloadCenter_Downloading");

        var progress = new Progress<ProgressReport>(report =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ImportUrlProgress = report.Percent;
                ImportUrlStatus = report.Message;
            });
        });

        try
        {
            var resolution = SelectedConflictResolutionIndex == 1
                ? ImportConflictResolution.Overwrite
                : ImportConflictResolution.Skip;

            var result = await _communityDataService.ImportPackageFromFileOrUrlAsync(DirectUrl.Trim(), resolution, progress);

            _soundService.PlaySuccessSound();
            var successMsg = string.Format(
                Loc.GetString("DownloadCenter_InstallSuccess"),
                result.TotalImported,
                result.TotalUpdated,
                result.TotalSkipped);

            _toastService.ShowSuccess(Loc.GetString("Toast_SuccessTitle"), successMsg);
            DirectUrl = string.Empty;

            await LoadCatalogAsync(forceRefresh: false);
        }
        catch (Exception ex)
        {
            _soundService.PlayErrorSound();
            _toastService.ShowError(Loc.GetString("Toast_ErrorTitle"), ex.Message);
        }
        finally
        {
            IsImportingUrl = false;
        }
    }

    public async Task<string> GenerateExportPackageJsonAsync()
    {
        var tags = ExportTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var metadata = new CommunityPackageMetadataDto
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Name = string.IsNullOrWhiteSpace(ExportName) ? "Custom Package" : ExportName.Trim(),
            Description = ExportDescription.Trim(),
            Author = string.IsNullOrWhiteSpace(ExportAuthor) ? "Lexora User" : ExportAuthor.Trim(),
            Category = ExportCategory,
            TargetExam = ExportExam,
            Level = ExportLevel,
            Version = "1.0.0",
            Tags = tags,
            License = "CC-BY-4.0",
            UpdatedAt = DateTime.UtcNow
        };

        return await _communityDataService.ExportPackageToJsonAsync(metadata);
    }

    public async Task<ImportResult> ImportFromRawJsonAsync(string rawJson, ImportConflictResolution resolution)
    {
        var result = await _communityDataService.ImportPackageFromFileOrUrlAsync(rawJson, resolution);
        await LoadCatalogAsync(forceRefresh: false);
        return result;
    }

    public void Dispose()
    {
        _localizationService.LanguageChanged -= OnLanguageChanged;
    }
}
