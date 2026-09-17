using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Presentation.Services;

public static class LocalizationHelper
{
    public static string GetString(string key)
    {
        var loc = App.Current.Services.GetService<ILocalizationService>();
        return loc?.GetString(key) ?? key;
    }
}
