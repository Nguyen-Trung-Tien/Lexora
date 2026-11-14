namespace Lexora.Presentation.Services;

/// <summary>
/// Defines a contract for Views (Pages/Dialogs) that react to real-time localization changes
/// and re-evaluate their bindings and localized text representations.
/// </summary>
public interface ILocalizableView
{
    void OnLanguageChanged();
}
