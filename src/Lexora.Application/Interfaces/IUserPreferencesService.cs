using Lexora.Application.DTOs;

namespace Lexora.Application.Interfaces;

public interface IUserPreferencesService
{
    UserPreferences GetPreferences();
    Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    event EventHandler<UserPreferences>? PreferencesChanged;
}
