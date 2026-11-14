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

public class NavigationService : INavigationService
{
    private sealed record NavigationRequest(Type PageType, object? Parameter, NavigationTransitionInfo? TransitionInfo, bool Force);

    private Frame? _frame;
    private Type? _currentTargetPageType;
    private bool _isNavigating;
    private NavigationRequest? _pendingRequest;

    public event EventHandler<Type>? Navigated;

    public Type? CurrentPageType => _currentTargetPageType ?? _frame?.CurrentSourcePageType ?? _frame?.Content?.GetType();
    public bool HasPendingNavigation => _pendingRequest != null;
    public bool IsNavigating => _isNavigating;

    private readonly Microsoft.UI.Xaml.Navigation.NavigatedEventHandler _navigatedHandler;
    private readonly Microsoft.UI.Xaml.Navigation.NavigationFailedEventHandler _navigationFailedHandler;
    private readonly Microsoft.UI.Xaml.Navigation.NavigationStoppedEventHandler _navigationStoppedHandler;

    public NavigationService()
    {
        _navigatedHandler = (s, e) =>
        {
            _isNavigating = false;
            if (e.SourcePageType != null)
            {
                _currentTargetPageType = e.SourcePageType;
                Navigated?.Invoke(this, e.SourcePageType);
            }
            ProcessPendingNavigation();
        };

        _navigationFailedHandler = (s, e) =>
        {
            e.Handled = true;
            _isNavigating = false;
            _currentTargetPageType = _frame?.CurrentSourcePageType ?? _frame?.Content?.GetType();
            System.Diagnostics.Debug.WriteLine($"[NavigationService] Frame.NavigationFailed: {e.SourcePageType} -> {e.Exception}");
            App.LogCrash($"NavigationService.NavigationFailed({e.SourcePageType?.Name})", e.Exception);
            ProcessPendingNavigation();
        };

        _navigationStoppedHandler = (s, e) =>
        {
            _isNavigating = false;
            ProcessPendingNavigation();
        };
    }

    public void Initialize(Frame frame)
    {
        if (_frame != null)
        {
            _frame.Navigated -= _navigatedHandler;
            _frame.NavigationFailed -= _navigationFailedHandler;
            _frame.NavigationStopped -= _navigationStoppedHandler;
        }

        _frame = frame;
        _frame.Navigated += _navigatedHandler;
        _frame.NavigationFailed += _navigationFailedHandler;
        _frame.NavigationStopped += _navigationStoppedHandler;
    }

    public bool Navigate(Type pageType, object? parameter = null, NavigationTransitionInfo? transitionInfo = null, bool force = false)
    {
        if (_frame == null)
        {
            System.Diagnostics.Debug.WriteLine($"[NavigationService] Cannot navigate to {pageType.Name}: _frame has not been initialized.");
            return false;
        }

        if (_frame.DispatcherQueue != null && !_frame.DispatcherQueue.HasThreadAccess)
        {
            bool dispatchedResult = false;
            _frame.DispatcherQueue.TryEnqueue(() =>
            {
                dispatchedResult = Navigate(pageType, parameter, transitionInfo, force);
            });
            return dispatchedResult;
        }

        // Avoid redundant navigation to current active/target page if parameter hasn't changed
        if (!force && parameter == null && CurrentPageType == pageType)
        {
            return false;
        }

        // If a navigation is currently in-flight, coalesce/queue this request instead of re-entering Frame.Navigate
        if (_isNavigating)
        {
            _currentTargetPageType = pageType;
            _pendingRequest = new NavigationRequest(pageType, parameter, transitionInfo, force);
            return true;
        }

        try
        {
            _isNavigating = true;
            _currentTargetPageType = pageType;
            transitionInfo ??= new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight };
            bool result = _frame.Navigate(pageType, parameter, transitionInfo);
            if (!result)
            {
                _isNavigating = false;
                _currentTargetPageType = _frame.CurrentSourcePageType ?? _frame.Content?.GetType();
            }
            return result;
        }
        catch (Exception ex)
        {
            _isNavigating = false;
            _currentTargetPageType = _frame.CurrentSourcePageType ?? _frame.Content?.GetType();
            System.Diagnostics.Debug.WriteLine($"[NavigationService] Navigation failed to {pageType.Name}: {ex}");
            App.LogCrash($"NavigationService.Navigate({pageType.Name})", ex);
            ProcessPendingNavigation();
            return false;
        }
    }

    private void ProcessPendingNavigation()
    {
        if (_pendingRequest == null) return;

        var req = _pendingRequest;
        _pendingRequest = null;

        if (_frame?.DispatcherQueue != null)
        {
            _frame.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                Navigate(req.PageType, req.Parameter, req.TransitionInfo, req.Force);
            });
        }
        else
        {
            Navigate(req.PageType, req.Parameter, req.TransitionInfo, req.Force);
        }
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
        try
        {
            if (CanGoBack)
            {
                _frame?.GoBack();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NavigationService] GoBack failed: {ex.Message}");
        }
    }
}
