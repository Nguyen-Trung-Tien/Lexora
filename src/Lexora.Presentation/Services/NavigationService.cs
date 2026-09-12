using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Services;

public interface INavigationService
{
    void Initialize(Frame frame);
    bool Navigate(Type pageType, object? parameter = null);
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

    public bool Navigate(Type pageType, object? parameter = null)
    {
        if (_frame == null) return false;
        if (_frame.Content?.GetType() == pageType) return false;

        return _frame.Navigate(pageType, parameter);
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
