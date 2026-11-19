using System.ComponentModel;
using System.Windows.Input;
using Lexora.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views.Common;

public sealed partial class PaginationBarControl : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public static readonly DependencyProperty CurrentPageProperty =
        DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(PaginationBarControl),
            new PropertyMetadata(1, OnPagingPropertyChanged));

    public static readonly DependencyProperty TotalPagesProperty =
        DependencyProperty.Register(nameof(TotalPages), typeof(int), typeof(PaginationBarControl),
            new PropertyMetadata(1, OnPagingPropertyChanged));

    public static readonly DependencyProperty TotalItemsProperty =
        DependencyProperty.Register(nameof(TotalItems), typeof(int), typeof(PaginationBarControl),
            new PropertyMetadata(-1, OnPagingPropertyChanged));

    public static readonly DependencyProperty FirstCommandProperty =
        DependencyProperty.Register(nameof(FirstCommand), typeof(ICommand), typeof(PaginationBarControl), new PropertyMetadata(null));

    public static readonly DependencyProperty PreviousCommandProperty =
        DependencyProperty.Register(nameof(PreviousCommand), typeof(ICommand), typeof(PaginationBarControl), new PropertyMetadata(null));

    public static readonly DependencyProperty NextCommandProperty =
        DependencyProperty.Register(nameof(NextCommand), typeof(ICommand), typeof(PaginationBarControl), new PropertyMetadata(null));

    public static readonly DependencyProperty LastCommandProperty =
        DependencyProperty.Register(nameof(LastCommand), typeof(ICommand), typeof(PaginationBarControl), new PropertyMetadata(null));

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public int TotalItems
    {
        get => (int)GetValue(TotalItemsProperty);
        set => SetValue(TotalItemsProperty, value);
    }

    public ICommand? FirstCommand
    {
        get => (ICommand?)GetValue(FirstCommandProperty);
        set => SetValue(FirstCommandProperty, value);
    }

    public ICommand? PreviousCommand
    {
        get => (ICommand?)GetValue(PreviousCommandProperty);
        set => SetValue(PreviousCommandProperty, value);
    }

    public ICommand? NextCommand
    {
        get => (ICommand?)GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }

    public ICommand? LastCommand
    {
        get => (ICommand?)GetValue(LastCommandProperty);
        set => SetValue(LastCommandProperty, value);
    }

    public event EventHandler? FirstRequested;
    public event EventHandler? PreviousRequested;
    public event EventHandler? NextRequested;
    public event EventHandler? LastRequested;

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < Math.Max(1, TotalPages);
    public Visibility TotalItemsVisibility => TotalItems >= 0 ? Visibility.Visible : Visibility.Collapsed;

    private ILocalizationService? _loc;

    public string ComputedPageSummary
    {
        get
        {
            var isEn = _loc?.CurrentLanguage == "en-US";
            var safeTotal = Math.Max(1, TotalPages);
            var safeCurrent = Math.Clamp(CurrentPage, 1, safeTotal);
            return isEn ? $"Page {safeCurrent} / {safeTotal}" : $"Trang {safeCurrent} / {safeTotal}";
        }
    }

    public string ComputedTotalSummary
    {
        get
        {
            if (TotalItems < 0) return string.Empty;
            var isEn = _loc?.CurrentLanguage == "en-US";
            return isEn ? $"• {TotalItems} items" : $"• {TotalItems} mục";
        }
    }

    public string PreviousLabel => _loc?.CurrentLanguage == "en-US" ? "Prev" : "Trước";
    public string NextLabel => _loc?.CurrentLanguage == "en-US" ? "Next" : "Sau";
    public string FirstTooltip => _loc?.CurrentLanguage == "en-US" ? "First page" : "Trang đầu";
    public string PreviousTooltip => _loc?.CurrentLanguage == "en-US" ? "Previous page" : "Trang trước";
    public string NextTooltip => _loc?.CurrentLanguage == "en-US" ? "Next page" : "Trang sau";
    public string LastTooltip => _loc?.CurrentLanguage == "en-US" ? "Last page" : "Trang cuối";

    public PaginationBarControl()
    {
        _loc = App.Current?.Services?.GetService<ILocalizationService>();
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (_loc != null) _loc.LanguageChanged += OnLanguageChanged;
        };
        Unloaded += (_, _) =>
        {
            if (_loc != null) _loc.LanguageChanged -= OnLanguageChanged;
        };
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(NotifyAllPropertiesChanged);
    }

    private static void OnPagingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PaginationBarControl ctrl)
        {
            ctrl.NotifyAllPropertiesChanged();
        }
    }

    private void NotifyAllPropertiesChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    private void BtnFirst_Click(object sender, RoutedEventArgs e)
    {
        if (FirstCommand?.CanExecute(null) == true) FirstCommand.Execute(null);
        FirstRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BtnPrev_Click(object sender, RoutedEventArgs e)
    {
        if (PreviousCommand?.CanExecute(null) == true) PreviousCommand.Execute(null);
        PreviousRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        if (NextCommand?.CanExecute(null) == true) NextCommand.Execute(null);
        NextRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BtnLast_Click(object sender, RoutedEventArgs e)
    {
        if (LastCommand?.CanExecute(null) == true) LastCommand.Execute(null);
        LastRequested?.Invoke(this, EventArgs.Empty);
    }
}
