using Prayer.Models;

namespace Prayer.Services;

public interface ISettingsService
{
    Task<UserSetting> GetSettingsAsync();
    Task SaveSettingsAsync(UserSetting setting);
    Task<List<ManualOverride>> GetOverridesAsync();
    Task SaveOverridesAsync(IEnumerable<ManualOverride> overrides);
    void UpdateAutostartRegistry(bool enable);
}
