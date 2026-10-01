using Lexora.Application.DTOs;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class AssignTopicDialog : ContentDialog
{
    public ILocalizationService Loc { get; }
    public Guid? SelectedTopicId { get; private set; }

    public AssignTopicDialog(IEnumerable<TopicDto> availableTopics, int wordCount)
    {
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        InitializeComponent();

        if (App.Current?.Services?.GetService<IThemeService>() is { } themeService && themeService.CurrentTheme != ElementTheme.Default)
        {
            RequestedTheme = themeService.CurrentTheme;
        }
        else if (App.Current?.MainWindow?.Content is FrameworkElement rootElem && rootElem.ActualTheme != ElementTheme.Default)
        {
            RequestedTheme = rootElem.ActualTheme;
        }

        Title = Loc.GetString("Vocab_AssignToTopicTitle");
        PrimaryButtonText = Loc.GetString("Action_Save");
        CloseButtonText = Loc.GetString("Action_Cancel");

        PromptTextBlock.Text = string.Format(Loc.GetString("Vocab_AssignToTopicPrompt"), wordCount);

        var topicList = availableTopics.Where(t => t.Id != Guid.Empty).ToList();
        TopicsComboBox.ItemsSource = topicList;
        if (topicList.Count > 0)
        {
            TopicsComboBox.SelectedIndex = 0;
            SelectedTopicId = topicList[0].Id;
        }

        PrimaryButtonClick += AssignTopicDialog_PrimaryButtonClick;
    }

    private void TopicsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TopicsComboBox.SelectedItem is TopicDto selected)
        {
            SelectedTopicId = selected.Id;
            DialogInfoBar.IsOpen = false;
        }
    }

    private void AssignTopicDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (TopicsComboBox.SelectedItem is not TopicDto selected || selected.Id == Guid.Empty)
        {
            args.Cancel = true;
            DialogInfoBar.Message = Loc.GetString("Storage_Dialog_SelectRequired");
            DialogInfoBar.IsOpen = true;
            return;
        }

        SelectedTopicId = selected.Id;
    }
}
