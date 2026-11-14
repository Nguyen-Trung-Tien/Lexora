using System.Globalization;

namespace Lexora.Presentation.Services;

public partial class LocalizationService : ILocalizationService
{
    private string _currentLanguage = "vi-VN";
    private readonly Dictionary<string, Dictionary<string, string>> _resources = InitializeResources();
    private readonly Lexora.Application.Interfaces.IUserPreferencesService _preferencesService;

    public LocalizationService() : this(new FallbackPreferencesService())
    {
    }

    public LocalizationService(Lexora.Application.Interfaces.IUserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
        var prefs = _preferencesService.GetPreferences();
        var savedLang = prefs.LanguageCode;
        if (_resources.ContainsKey(savedLang))
        {
            _currentLanguage = savedLang;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(savedLang);
                CultureInfo.CurrentCulture = new CultureInfo(savedLang);
            }
            catch { /* fallback to default */ }
        }
    }

    private static Dictionary<string, Dictionary<string, string>> InitializeResources()
    {
        var vi = new Dictionary<string, string>(StringComparer.Ordinal);
        var en = new Dictionary<string, string>(StringComparer.Ordinal);

        RegisterCommon(vi, en);
        RegisterDashboard(vi, en);
        RegisterDialogs(vi, en);
        RegisterGrammar(vi, en);
        RegisterHelp(vi, en);
        RegisterHomophones(vi, en);
        RegisterKnowledgeGraph(vi, en);
        RegisterListeningLab(vi, en);
        RegisterNavigation(vi, en);
        RegisterReview(vi, en);
        RegisterRoadmap(vi, en);
        RegisterSettings(vi, en);
        RegisterSmartImport(vi, en);
        RegisterStorage(vi, en);
        RegisterTest(vi, en);
        RegisterVocabulary(vi, en);
        RegisterPhrases(vi, en);

        return new Dictionary<string, Dictionary<string, string>>
        {
            ["vi-VN"] = vi,
            ["en-US"] = en
        };
    }

    public string CurrentLanguage => _currentLanguage;
    public event EventHandler? LanguageChanged;

    public string this[string key] => GetString(key);

    public void SetLanguage(string languageCode)
    {
        if (_resources.ContainsKey(languageCode))
        {
            _currentLanguage = languageCode;
            var culture = new CultureInfo(languageCode);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;

            var currentPrefs = _preferencesService.GetPreferences();
            if (currentPrefs.LanguageCode != languageCode)
            {
                _ = _preferencesService.SavePreferencesAsync(currentPrefs with { LanguageCode = languageCode });
            }

            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string GetString(string key)
    {
        if (_resources.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var val))
        {
            return val;
        }

        // Fallback to en-US
        if (_resources["en-US"].TryGetValue(key, out var fallbackVal))
        {
            return fallbackVal;
        }

        return key;
    }

    private sealed class FallbackPreferencesService : Lexora.Application.Interfaces.IUserPreferencesService
    {
        public Lexora.Application.DTOs.UserPreferences GetPreferences() => new();
        public Task SavePreferencesAsync(Lexora.Application.DTOs.UserPreferences preferences, CancellationToken cancellationToken = default) => Task.CompletedTask;
#pragma warning disable CS0067
        public event EventHandler<Lexora.Application.DTOs.UserPreferences>? PreferencesChanged;
#pragma warning restore CS0067
    }
}
