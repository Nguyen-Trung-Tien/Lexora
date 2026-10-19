namespace Lexora.Presentation.Services;

/// <summary>
/// Defines a contract for Views (Pages/Dialogs) that react to real-time theme changes
/// and re-evaluate their dynamic brushes, custom controls, and bindings.
/// </summary>
public interface IThemeAwareView
{
    void OnThemeChanged();
}
