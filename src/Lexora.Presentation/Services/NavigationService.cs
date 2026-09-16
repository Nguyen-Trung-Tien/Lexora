using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Lexora.Presentation.Services;

public interface INavigationService
{
    void Initialize(Frame frame);
    bool Navigate(Type pageType, object? parameter = null, NavigationTransitionInfo? transitionInfo = null, bool force = false);
    void Reload(NavigationTransitionInfo? transitionInfo = null);
    bool CanGoBack { get; }
    void GoBack();
    event EventHandler<Type>? Navigated;
}

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public event EventHandler<Type>? Navigated;

    public void Initialize(Frame frame)
    {
        _frame = frame;
        _frame.Navigated += (s, e) =>
        {
            if (e.SourcePageType != null)
            {
                Navigated?.Invoke(this, e.SourcePageType);
            }
        };
    }

    public bool Navigate(Type pageType, object? parameter = null, NavigationTransitionInfo? transitionInfo = null, bool force = false)
    {
        if (_frame == null) return false;
        if (!force && _frame.Content?.GetType() == pageType && parameter == null) return false;

        transitionInfo ??= new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight };
        return _frame.Navigate(pageType, parameter, transitionInfo);
    }

    public void Reload(NavigationTransitionInfo? transitionInfo = null)
    {
        if (_frame?.Content is Page currentPage)
        {
            Navigate(currentPage.GetType(), force: true, transitionInfo: transitionInfo ?? new SuppressNavigationTransitionInfo());
        }
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void GoBack()
    {
        if (CanGoBack)
        {
            _frame?.GoBack();
        }
    }
}
