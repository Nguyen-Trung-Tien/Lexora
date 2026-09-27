using Lexora.Application.DTOs;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class TopicEditorDialog : ContentDialog
{
    public ILocalizationService Loc { get; }
    public TopicCreateOrUpdateDto? ResultDto { get; private set; }

    private readonly List<TopicIconOption> _iconOptions;
    private string _selectedIcon = "\uE8EC";

    public TopicEditorDialog()
    {
        Loc = App.Current.Services.GetRequiredService<ILocalizationService>();
        InitializeComponent();

        if (App.Current?.Services?.GetService<IThemeService>() is { } themeService)
        {
            RequestedTheme = themeService.CurrentTheme;
        }

        Title = Loc.GetString("TopicEditor_TitleAdd");
        PrimaryButtonText = Loc.GetString("TopicEditor_Save");
        CloseButtonText = Loc.GetString("TopicEditor_Cancel");
        DialogInfoBar.Title = Loc.GetString("TopicEditor_ValidationTitle");

        _iconOptions =
        [
            new() { Glyph = "\uE8EC", Name = Loc.GetString("TopicIcon_General") },
            new() { Glyph = "\uE82D", Name = Loc.GetString("TopicIcon_Education") },
            new() { Glyph = "\uE8F1", Name = Loc.GetString("TopicIcon_Units") },
            new() { Glyph = "\uE825", Name = Loc.GetString("TopicIcon_Business") },
            new() { Glyph = "\uE709", Name = Loc.GetString("TopicIcon_Travel") },
            new() { Glyph = "\uE7F4", Name = Loc.GetString("TopicIcon_Tech") },
            new() { Glyph = "\uE9CA", Name = Loc.GetString("TopicIcon_Science") },
            new() { Glyph = "\uE707", Name = Loc.GetString("TopicIcon_Food") },
            new() { Glyph = "\uE7C3", Name = Loc.GetString("TopicIcon_Sports") },
            new() { Glyph = "\uE734", Name = Loc.GetString("TopicIcon_Goal") },
            new() { Glyph = "\uE8BD", Name = Loc.GetString("TopicIcon_Chat") },
            new() { Glyph = "\uE7BE", Name = Loc.GetString("TopicIcon_Health") }
        ];

        _iconOptions[0].IsSelected = true;
        _selectedIcon = _iconOptions[0].Glyph;
        IconGridView.ItemsSource = _iconOptions;
        IconGridView.SelectedIndex = 0;

        PrimaryButtonClick += TopicEditorDialog_PrimaryButtonClick;
    }

    private void IconGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var opt in _iconOptions)
        {
            opt.IsSelected = false;
        }

        if (IconGridView.SelectedItem is TopicIconOption selected)
        {
            selected.IsSelected = true;
            _selectedIcon = selected.Glyph;
        }
    }


    private void TopicEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var name = NameTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            args.Cancel = true;
            DialogInfoBar.Message = Loc.GetString("TopicEditor_ErrNameRequired");
            DialogInfoBar.IsOpen = true;
            NameTextBox.Focus(FocusState.Programmatic);
            return;
        }

        var description = DescriptionTextBox.Text?.Trim();

        ResultDto = new TopicCreateOrUpdateDto(
            Name: name,
            Description: string.IsNullOrWhiteSpace(description) ? null : description,
            IconGlyph: _selectedIcon,
            ColorHex: "#0078D4"
        );
    }
}

public partial class TopicIconOption : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public string Glyph { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(BorderThickness));
                OnPropertyChanged(nameof(BackgroundBrush));
                OnPropertyChanged(nameof(BorderBrush));
                OnPropertyChanged(nameof(ForegroundBrush));
                OnPropertyChanged(nameof(BadgeVisibility));
            }
        }
    }

    public Thickness BorderThickness => IsSelected ? new Thickness(2) : new Thickness(1);

    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush _fallbackTransparent = new(Microsoft.UI.Colors.Transparent);

    private static Microsoft.UI.Xaml.Media.Brush GetResourceBrush(string key)
    {
        return Services.ThemeResourceHelper.GetThemeBrush(key) ?? _fallbackTransparent;
    }

    public Microsoft.UI.Xaml.Media.Brush BackgroundBrush => IsSelected
        ? GetResourceBrush("AccentFillColorDefaultBrush")
        : GetResourceBrush("SubtleFillColorSecondaryBrush");

    public Microsoft.UI.Xaml.Media.Brush BorderBrush => IsSelected
        ? GetResourceBrush("AccentFillColorDefaultBrush")
        : GetResourceBrush("CardStrokeColorDefaultBrush");

    public Microsoft.UI.Xaml.Media.Brush ForegroundBrush => IsSelected
        ? GetResourceBrush("TextOnAccentFillColorPrimaryBrush")
        : GetResourceBrush("AccentTextFillColorPrimaryBrush");


    public Visibility BadgeVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;
}

