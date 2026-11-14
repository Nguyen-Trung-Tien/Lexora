using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;
using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views.Phrases;

public sealed partial class PhrasesPage : Page, ILocalizableView, IThemeAwareView
{
    private readonly PhrasesViewModel _viewModel;
    private readonly ILocalizationService _loc;
    private readonly IConversationPhraseService _phraseService;

    public PhrasesViewModel ViewModel => _viewModel;
    public ILocalizationService Loc => _loc;

    public PhrasesPage() : this(
        App.Current.Services.GetRequiredService<PhrasesViewModel>(),
        App.Current.Services.GetRequiredService<ILocalizationService>(),
        App.Current.Services.GetRequiredService<IConversationPhraseService>())
    {
    }

    public PhrasesPage(
        PhrasesViewModel viewModel,
        ILocalizationService loc,
        IConversationPhraseService phraseService)
    {
        NavigationCacheMode = NavigationCacheMode.Required;
        _viewModel = viewModel;
        _loc = loc;
        _phraseService = phraseService;

        InitializeComponent();

        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadDataAsync();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (IsLoaded)
        {
            await _viewModel.LoadDataAsync();
        }
    }

    public void OnLanguageChanged()
    {
        Bindings.Update();
    }

    public void OnThemeChanged()
    {
        Bindings.Update();
    }

    private async void BtnAddPhrase_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PhraseEditorDialog(_loc)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.CreatedResult != null)
        {
            try
            {
                var created = await _phraseService.CreateAsync(dialog.CreatedResult);
                _viewModel.OnPhraseSaved(created, isNew: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PhrasesPage] Create phrase error: {ex.Message}");
            }
        }
    }

    private async void CardEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ConversationPhraseDto phrase)
        {
            var dialog = new PhraseEditorDialog(_loc, phrase)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.UpdatedResult != null)
            {
                try
                {
                    var updated = await _phraseService.UpdateAsync(dialog.UpdatedResult);
                    _viewModel.OnPhraseSaved(updated, isNew: false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PhrasesPage] Update phrase error: {ex.Message}");
                }
            }
        }
    }

    private async void CardDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ConversationPhraseDto phrase)
        {
            var confirmDialog = new ContentDialog
            {
                Title = _loc.GetString("Phrases_ConfirmDeleteTitle"),
                Content = _loc.GetString("Phrases_ConfirmDeleteMessage"),
                PrimaryButtonText = _loc.GetString("Action_Delete"),
                CloseButtonText = _loc.GetString("Action_Cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _viewModel.DeletePhraseAsync(phrase);
            }
        }
    }

    private async void CardFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ConversationPhraseDto phrase)
        {
            await _viewModel.ToggleFavoriteAsync(phrase);
        }
    }

    private async void CardPlayPrompt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ConversationPhraseDto phrase)
        {
            await _viewModel.PlayPromptAudioAsync(phrase);
        }
    }

    private async void CardPlayResponse_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ConversationPhraseDto phrase)
        {
            await _viewModel.PlayResponseAudioAsync(phrase);
        }
    }

    private void CardCopyPrompt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ConversationPhraseDto phrase)
        {
            _viewModel.CopyText(phrase.PromptSentence);
        }
    }

    private void CardCopyResponse_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ConversationPhraseDto phrase)
        {
            _viewModel.CopyText(phrase.SampleResponse);
        }
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        await _viewModel.RefreshPhrasesAsync();
    }

    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && string.IsNullOrEmpty(sender.Text))
        {
            await _viewModel.RefreshPhrasesAsync();
        }
    }

    private async void CategoryFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await _viewModel.RefreshPhrasesAsync();
    }

    private async void LevelFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LevelFilterBox.SelectedItem is ComboBoxItem item)
        {
            var tag = item.Tag?.ToString();
            if (string.Equals(tag, "All", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(tag))
            {
                _viewModel.SelectedLevel = null;
            }
            else if (Enum.TryParse<CefrLevel>(tag, out var parsed))
            {
                _viewModel.SelectedLevel = parsed;
            }
            await _viewModel.RefreshPhrasesAsync();
        }
    }

    private async void FavoritesToggle_Checked(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshPhrasesAsync();
    }

    private async void FavoritesToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshPhrasesAsync();
    }

    private async void BtnClearFilters_Click(object sender, RoutedEventArgs e)
    {
        LevelFilterBox.SelectedIndex = 0;
        FavoritesToggle.IsChecked = false;
        await _viewModel.ClearFiltersAsync();
    }
}
