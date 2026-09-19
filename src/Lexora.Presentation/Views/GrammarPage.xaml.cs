using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views;

public sealed partial class GrammarPage : Page
{
    public GrammarViewModel ViewModel { get; }
    public Lexora.Presentation.Services.ILocalizationService Loc { get; }

    public GrammarPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<GrammarViewModel>();
        Loc = App.Current.Services.GetRequiredService<Lexora.Presentation.Services.ILocalizationService>();

        Loc.LanguageChanged += OnLanguageChanged;
        Unloaded += (s, e) => Loc.LanguageChanged -= OnLanguageChanged;

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _searchDebounceTimer.Tick += async (s, e) =>
        {
            _searchDebounceTimer.Stop();
            await ViewModel.LoadRulesAsync();
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => Bindings.Update());
    }

    private readonly DispatcherTimer _searchDebounceTimer;

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        _ = ViewModel.LoadRulesAsync();
    }

    private async void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var dialog = new GrammarEditorDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = activeTheme
        };
        dialog.InitializeForCreate();

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
        {
            await ViewModel.AddRuleAsync(dialog.ResultDto);
        }
    }

    private async void EditRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRule == null) return;

        var activeTheme = (this.XamlRoot?.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
        var dialog = new GrammarEditorDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = activeTheme
        };
        dialog.InitializeForEdit(ViewModel.SelectedRule);

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.ResultDto != null)
        {
            await ViewModel.UpdateRuleAsync(dialog.ResultDto);
        }
    }

    private async void DeleteRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRule == null) return;

        var confirmDialog = new ContentDialog
        {
            Title = Loc.GetString("Grammar_DeleteTitle"),
            Content = string.Format(Loc.GetString("Grammar_DeleteConfirm"), ViewModel.SelectedRule.Title),
            PrimaryButtonText = Loc.GetString("Action_Delete"),
            CloseButtonText = Loc.GetString("Action_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteRuleAsync(ViewModel.SelectedRule.Id);
        }
    }

    private async void PracticeButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartPracticeAsync();
    }

    private void PracticeOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string opt)
        {
            ViewModel.SelectPracticeOption(opt);
        }
    }

    private void NextPracticeQuestion_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NextPracticeQuestion();
    }

    private void ExitPracticeButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExitPractice();
    }

    public InfoBarSeverity GetFeedbackSeverity(bool isCorrect)
    {
        return isCorrect ? InfoBarSeverity.Success : InfoBarSeverity.Error;
    }

    public string GetFeedbackTitle(bool isCorrect)
    {
        return isCorrect 
            ? (Loc.CurrentLanguage == "vi-VN" ? "Chính xác! 🎉" : "Correct! 🎉")
            : (Loc.CurrentLanguage == "vi-VN" ? "Chưa chính xác!" : "Incorrect!");
    }

    public Brush GetQuestionReviewBrush(bool? isCorrect)
    {
        string brushKey = isCorrect == true ? "SystemFillColorSuccessBrush" : "SystemFillColorCriticalBrush";
        return Services.ThemeResourceHelper.GetThemeBrush(this, brushKey) ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    public string GetQuestionReviewGlyph(bool? isCorrect)
    {
        return isCorrect == true ? "\uE73E" : "\uE711";
    }

    public string GetQuestionReviewStatusText(bool? isCorrect)
    {
        if (isCorrect == true)
        {
            return Loc.CurrentLanguage == "vi-VN" ? "Chính xác" : "Correct";
        }
        return Loc.CurrentLanguage == "vi-VN" ? "Chưa đúng" : "Incorrect";
    }

    private void AddRuleAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        AddRuleButton_Click(this, new RoutedEventArgs());
        args.Handled = true;
    }

    private void EditRuleAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.SelectedRule != null)
        {
            EditRuleButton_Click(this, new RoutedEventArgs());
            args.Handled = true;
        }
    }

    private void DeleteRuleAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.SelectedRule != null)
        {
            DeleteRuleButton_Click(this, new RoutedEventArgs());
            args.Handled = true;
        }
    }

    private void PracticeAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        PracticeButton_Click(this, new RoutedEventArgs());
        args.Handled = true;
    }

    private async void CopyStructureButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRule == null || string.IsNullOrWhiteSpace(ViewModel.SelectedRule.Structure)) return;

        try
        {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(ViewModel.SelectedRule.Structure);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);

            // Visual feedback: checkmark + "Đã sao chép!"
            IconCopyStructure.Glyph = "\uE73E";
            if (App.Current.Resources.TryGetValue("SystemFillColorSuccessBrush", out var successBrush) && successBrush is Microsoft.UI.Xaml.Media.Brush bSuccess)
            {
                IconCopyStructure.Foreground = bSuccess;
                TextCopyStructure.Foreground = bSuccess;
            }
            TextCopyStructure.Text = Loc.GetString("Action_Copied");

            await Task.Delay(1800);

            IconCopyStructure.Glyph = "\uE8C8";
            IconCopyStructure.ClearValue(FontIcon.ForegroundProperty);
            TextCopyStructure.Text = Loc.GetString("Action_Copy");
            TextCopyStructure.ClearValue(TextBlock.ForegroundProperty);
        }
        catch
        {
            // Clipboard fallback
        }
    }
}
