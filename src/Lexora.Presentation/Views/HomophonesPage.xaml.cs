using Windows.Storage.Pickers;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class HomophonesPage : Page, ILocalizableView
{
    public HomophonesViewModel ViewModel { get; }
    public ILocalizationService Loc { get; }
    private readonly INavigationService _navigationService;
    private readonly ILearningStorageService _storageService;
    private readonly Lexora.Application.Interfaces.IVocabularyService _vocabService;
    private readonly IToastNotificationService? _toastService;

    private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(220) };

    public HomophonesPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<HomophonesViewModel>();
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        _navigationService = App.Current.Services.GetRequiredService<INavigationService>();
        _storageService = App.Current.Services.GetRequiredService<ILearningStorageService>();
        _vocabService = App.Current.Services.GetRequiredService<Lexora.Application.Interfaces.IVocabularyService>();
        _toastService = App.Current.Services.GetService<IToastNotificationService>();

        InitializeComponent();

        _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Tick -= SearchDebounceTimer_Tick;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedGroup))
        {
            if (!Equals(GroupsListView.SelectedItem, ViewModel.SelectedGroup))
            {
                GroupsListView.SelectedItem = ViewModel.SelectedGroup;
            }
            UpdateDetailView();
        }
    }

    private void SearchDebounceTimer_Tick(object? sender, object e)
    {
        _searchDebounceTimer.Stop();
        ViewModel.SearchQuery = SearchBox.Text;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string searchTerm && !string.IsNullOrWhiteSpace(searchTerm))
        {
            SearchBox.Text = searchTerm;
            ViewModel.SearchQuery = searchTerm;
        }
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnPageLoaded;
        ApplyLocalization();
        await ViewModel.LoadGroupsAsync();
        UpdateDetailView();
    }

    public void OnLanguageChanged()
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            ApplyLocalization();
        }
        else
        {
            DispatcherQueue.TryEnqueue(ApplyLocalization);
        }
    }

    private void ApplyLocalization()
    {
        PageTitleText.Text = Loc.GetString("Homophones_Title");
        PageSubtitleText.Text = Loc.GetString("Homophones_Subtitle");
        TextAddGroup.Text = Loc.GetString("Homophones_AddGroup");
        TextSmartImport.Text = Loc.GetString("Homophones_SmartImport");
        TextExport.Text = Loc.GetString("Action_Export");
        SearchBox.PlaceholderText = Loc.GetString("Homophones_SearchPlaceholder");
        Bindings.Update();
        UpdateDetailView();
    }

    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();

            if (!string.IsNullOrWhiteSpace(sender.Text))
            {
                var suggestions = await ViewModel.GetSearchSuggestionsAsync(sender.Text);
                sender.ItemsSource = suggestions.Count > 0 ? suggestions : null;
            }
            else
            {
                sender.ItemsSource = null;
            }
        }
        else if (e.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
        {
            ViewModel.SearchQuery = sender.Text;
        }
    }

    private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SmartSearchSuggestion suggestion)
        {
            sender.Text = suggestion.Title;
            if (Guid.TryParse(suggestion.TargetType, out var groupId))
            {
                ViewModel.SelectGroupById(groupId);
                UpdateDetailView();
            }
            else
            {
                ViewModel.SearchQuery = suggestion.SearchTerm ?? suggestion.Title;
            }
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is SmartSearchSuggestion suggestion)
        {
            if (Guid.TryParse(suggestion.TargetType, out var groupId))
            {
                ViewModel.SelectGroupById(groupId);
                UpdateDetailView();
                return;
            }
        }

        var query = args.QueryText?.Trim();
        ViewModel.SearchQuery = query ?? string.Empty;
    }

    private void TypeFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeFilterBox.SelectedItem is ComboBoxItem item)
        {
            var tag = item.Tag?.ToString();
            if (tag == "All" || string.IsNullOrEmpty(tag))
            {
                ViewModel.SelectedTypeFilter = null;
            }
            else if (Enum.TryParse<HomophoneType>(tag, out var t))
            {
                ViewModel.SelectedTypeFilter = t;
            }
        }
    }

    private void LevelFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LevelFilterBox.SelectedItem is ComboBoxItem item)
        {
            var tag = item.Tag?.ToString();
            if (tag == "All" || string.IsNullOrEmpty(tag))
            {
                ViewModel.SelectedLevelFilter = null;
            }
            else if (Enum.TryParse<CefrLevel>(tag, out var l))
            {
                ViewModel.SelectedLevelFilter = l;
            }
        }
    }

    private void GroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GroupsListView.SelectedItem is HomophoneGroupDto group)
        {
            ViewModel.SelectedGroup = group;
            UpdateDetailView();
        }
    }

    private void UpdateDetailView()
    {
        var group = ViewModel.SelectedGroup;
        if (group == null)
        {
            DetailContentPanel.Visibility = Visibility.Collapsed;
            EmptyListPanel.Visibility = ViewModel.Groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        DetailContentPanel.Visibility = Visibility.Visible;
        EmptyListPanel.Visibility = Visibility.Collapsed;

        // Group Title & IPA
        DetailGroupTitle.Text = group.Title;
        DetailGroupIpa.Text = !string.IsNullOrWhiteSpace(group.PhoneticIpa) ? group.PhoneticIpa : "—";

        // Badges
        TextBadgeType.Text = group.Type switch
        {
            HomophoneType.ExactHomophone => Loc.GetString("Homophones_TypeExact"),
            HomophoneType.NearHomophone => Loc.GetString("Homophones_TypeNear"),
            HomophoneType.MinimalPair => Loc.GetString("Homophones_TypePair"),
            _ => group.Type.ToString()
        };
        TextBadgeLevel.Text = group.DifficultyLevel != CefrLevel.None ? $"CEFR {group.DifficultyLevel}" : Loc.GetString("Homophones_FilterAllLevels");
        TextBadgeExam.Text = LocalizationHelper.FormatExamType(group.ExamTag);

        // Tip
        if (!string.IsNullOrWhiteSpace(group.DifferentiationTip))
        {
            TipBanner.Visibility = Visibility.Visible;
            TextDifferentiationTip.Text = group.DifferentiationTip;
        }
        else
        {
            TipBanner.Visibility = Visibility.Collapsed;
        }

        // Comparison Cards
        if (group.Items.Count > 0)
        {
            var w1 = group.Items[0];
            Word1Term.Text = w1.Term;
            Word1Pos.Text = LocalizationHelper.FormatPartOfSpeech(w1.PartOfSpeech);
            Word1MeaningVi.Text = w1.MeaningVi;
            Word1Example.Text = !string.IsNullOrWhiteSpace(w1.ExampleSentence) ? w1.ExampleSentence : Loc.GetString("Homophones_NoExample");
            Word1Translation.Text = w1.ExampleTranslation ?? string.Empty;
            BtnListenWord1.Tag = w1.Term;
            BtnViewWord1.Tag = w1.Term;
            BtnSaveWord1.Tag = w1.Term;
        }

        if (group.Items.Count > 1)
        {
            var w2 = group.Items[1];
            Word2Term.Text = w2.Term;
            Word2Pos.Text = LocalizationHelper.FormatPartOfSpeech(w2.PartOfSpeech);
            Word2MeaningVi.Text = w2.MeaningVi;
            Word2Example.Text = !string.IsNullOrWhiteSpace(w2.ExampleSentence) ? w2.ExampleSentence : Loc.GetString("Homophones_NoExample");
            Word2Translation.Text = w2.ExampleTranslation ?? string.Empty;
            BtnListenWord2.Tag = w2.Term;
            BtnViewWord2.Tag = w2.Term;
            BtnSaveWord2.Tag = w2.Term;
        }
    }

    private async void BtnAddGroup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HomophoneEditorDialog(Loc)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsyncSafe();
        if (result == ContentDialogResult.Primary && dialog.CreatedGroupResult != null)
        {
            try
            {
                await ViewModel.SaveGroupAsync(dialog.CreatedGroupResult);
                UpdateDetailView();
                _toastService?.ShowSuccess(string.Format(Loc.GetString("Homophones_Toast_GroupSaved"), dialog.CreatedGroupResult.Title));
            }
            catch (Exception ex)
            {
                _toastService?.ShowError(ex.Message);
            }
        }
    }

    private async void BtnEditGroup_Click(object sender, RoutedEventArgs e)
    {
        var group = ViewModel.SelectedGroup;
        if (group == null) return;

        var dialog = new HomophoneEditorDialog(Loc, group)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsyncSafe();
        if (result == ContentDialogResult.Primary && dialog.UpdatedGroupResult != null)
        {
            try
            {
                await ViewModel.UpdateGroupAsync(dialog.UpdatedGroupResult);
                UpdateDetailView();
                _toastService?.ShowSuccess(string.Format(Loc.GetString("Homophones_Toast_GroupUpdated"), dialog.UpdatedGroupResult.Title));
            }
            catch (Exception ex)
            {
                _toastService?.ShowError(ex.Message);
            }
        }
    }

    private async void BtnDeleteGroup_Click(object sender, RoutedEventArgs e)
    {
        var group = ViewModel.SelectedGroup;
        if (group == null) return;

        var confirmDialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = Loc.GetString("Action_ConfirmDelete"),
            Content = string.Format(Loc.GetString("Homophones_ConfirmDeleteMessage"), group.Title),
            PrimaryButtonText = Loc.GetString("Action_Delete"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirmDialog.ShowAsyncSafe();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var title = group.Title;
                await ViewModel.DeleteGroupAsync(group.Id);
                UpdateDetailView();
                _toastService?.ShowSuccess(string.Format(Loc.GetString("Homophones_Toast_GroupDeleted"), title));
            }
            catch (Exception ex)
            {
                _toastService?.ShowError(ex.Message);
            }
        }
    }

    private async void BtnSmartImport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SmartImportDialog(fmt => ViewModel.GetSampleTemplate(fmt), isGrammarMode: false, isHomophoneMode: true)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsyncSafe();
        if (dialog.WasCommitted)
        {
            await ViewModel.LoadGroupsAsync();
            UpdateDetailView();
            var count = (dialog.CommitResult?.TotalImported ?? 0) + (dialog.CommitResult?.TotalUpdated ?? 0);
            _toastService?.ShowSuccess(string.Format(Loc.GetString("SmartImport_Toast_Success"), count));
        }
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e) => await ExportToFileAsync("json");
    private async void ExportCsv_Click(object sender, RoutedEventArgs e) => await ExportToFileAsync("csv");
    private async void ExportTxt_Click(object sender, RoutedEventArgs e) => await ExportToFileAsync("txt");

    private async Task ExportToFileAsync(string format)
    {
        try
        {
            var content = await ViewModel.ExportAsync(format);
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"Lexora_Homophones_{DateTime.Now:yyyyMMdd}"
            };

            var ext = format switch
            {
                "csv" => ".csv",
                "txt" => ".txt",
                _ => ".json"
            };
            picker.FileTypeChoices.Add($"Homophones Export ({ext})", [ext]);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, content);
                _toastService?.ShowSuccess(string.Format(Loc.GetString("Homophones_Toast_ExportSuccess"), file.Name));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomophonesPage] ExportToFileAsync error: {ex.Message}");
            _toastService?.ShowError(ex.Message);
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadGroupsAsync();
        UpdateDetailView();
    }

    private async void BtnListenWord1_Click(object sender, RoutedEventArgs e)
    {
        if (BtnListenWord1.Tag is string term)
        {
            await ViewModel.PlayAudioAsync(term);
        }
    }

    private async void BtnListenWord2_Click(object sender, RoutedEventArgs e)
    {
        if (BtnListenWord2.Tag is string term)
        {
            await ViewModel.PlayAudioAsync(term);
        }
    }

    private void ViewInVocab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string term && !string.IsNullOrWhiteSpace(term))
        {
            _navigationService.Navigate(typeof(VocabularyPage), term);
        }
    }

    private async void SaveToStorage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string term && !string.IsNullOrWhiteSpace(term))
        {
            try
            {
                var words = await _vocabService.SearchWordsAsync(search: term, pageSize: 5);
                var matched = words.FirstOrDefault(w => w.Term.Equals(term, StringComparison.OrdinalIgnoreCase)) ?? words.FirstOrDefault();
                if (matched != null)
                {
                    await _storageService.MoveWordToStorageAsync(matched.Id, StorageAdditionReason.Manual, "Added from Homophones");
                    _toastService?.ShowSuccess(string.Format(Loc.GetString("Homophones_SavedToStorageSuccess"), term), Loc.GetString("Common_Success"));
                }
                else
                {
                    var newWord = await _vocabService.CreateWordAsync(new WordCreateOrUpdateDto(
                        Id: null,
                        Term: term,
                        PhoneticUk: null,
                        PhoneticUs: null,
                        AudioUkPath: null,
                        AudioUsPath: null,
                        Level: CefrLevel.B1,
                        PrimaryPartOfSpeech: PartOfSpeech.Noun,
                        Notes: null,
                        Meanings: new List<WordMeaningCreateDto>
                        {
                            new(PartOfSpeech.Noun, "(Đồng âm)", null, null, new List<ExampleSentenceCreateDto>())
                        },
                        TopicIds: new List<Guid>(),
                        ExamTagIds: new List<Guid>()
                    ));
                    await _storageService.MoveWordToStorageAsync(newWord.Id, StorageAdditionReason.Manual, "Added from Homophones");
                    _toastService?.ShowSuccess(string.Format(Loc.GetString("Homophones_SavedToStorageSuccess"), term), Loc.GetString("Common_Success"));
                }
            }
            catch (Exception ex)
            {
                _toastService?.ShowError(ex.Message, Loc.GetString("Common_Error"));
            }
        }
    }
}
