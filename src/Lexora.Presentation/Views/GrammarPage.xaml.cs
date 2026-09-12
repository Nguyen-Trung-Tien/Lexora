using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Lexora.Presentation.Views;

public sealed partial class GrammarPage : Page
{
    public GrammarViewModel ViewModel { get; }

    public GrammarPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<GrammarViewModel>();

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _searchDebounceTimer.Tick += async (s, e) =>
        {
            _searchDebounceTimer.Stop();
            await ViewModel.LoadRulesAsync();
        };
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
            Title = "Delete Grammar Rule",
            Content = $"Are you sure you want to permanently delete \"{ViewModel.SelectedRule.Title}\"?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
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
        return isCorrect ? "Correct!" : "Incorrect!";
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
}
