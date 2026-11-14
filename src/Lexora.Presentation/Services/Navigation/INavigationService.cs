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
    Type? CurrentPageType { get; }
    bool HasPendingNavigation { get; }
    bool IsNavigating { get; }
}
