namespace Lexora.Presentation.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    void SetLanguage(string languageCode);
    string GetString(string key);
    string this[string key] { get; }
    event EventHandler? LanguageChanged;
}
