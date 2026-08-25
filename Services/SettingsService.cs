using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Prayer.Data;
using Prayer.Models;
using System.Diagnostics;
using System.IO;

namespace Prayer.Services;

public class SettingsService : ISettingsService
{
    private readonly PrayerDbContext _dbContext;
    private const string AppRegistryKeyName = "PrayerFocusLock";

    public SettingsService(PrayerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserSetting> GetSettingsAsync()
    {
        var setting = await _dbContext.UserSettings.FirstOrDefaultAsync();
        if (setting == null)
        {
            setting = new UserSetting();
            _dbContext.UserSettings.Add(setting);
            await _dbContext.SaveChangesAsync();
        }
        return setting;
    }

    public async Task SaveSettingsAsync(UserSetting setting)
    {
        var existing = await _dbContext.UserSettings.FirstOrDefaultAsync(s => s.Id == setting.Id);
        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(setting);
        }
        else
        {
            _dbContext.UserSettings.Add(setting);
        }
        await _dbContext.SaveChangesAsync();

        UpdateAutostartRegistry(setting.AutostartEnabled);
    }

    public async Task<List<ManualOverride>> GetOverridesAsync()
    {
        return await _dbContext.ManualOverrides.ToListAsync();
    }

    public async Task SaveOverridesAsync(IEnumerable<ManualOverride> overrides)
    {
        _dbContext.ManualOverrides.RemoveRange(_dbContext.ManualOverrides);
        _dbContext.ManualOverrides.AddRange(overrides);
        await _dbContext.SaveChangesAsync();
    }

    public void UpdateAutostartRegistry(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppRegistryKeyName, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(AppRegistryKeyName, false);
            }
        }
        catch
        {
            // Suppress registry permission or policy exceptions gracefully
        }
    }
}
