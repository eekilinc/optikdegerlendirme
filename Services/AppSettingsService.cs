using System;
using System.IO;
using System.Text.Json;
using OptikFormApp.Models;

namespace OptikFormApp.Services;

public class AppSettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OptikFormApp");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static AppSettings? _cachedSettings;

    public async Task<AppSettings> LoadAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        try
        {
            Directory.CreateDirectory(SettingsDir);

            if (File.Exists(SettingsPath))
            {
                var json = await File.ReadAllTextAsync(SettingsPath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                _cachedSettings = new AppSettings();
                await SaveAsync(_cachedSettings);
            }
        }
        catch (Exception)
        {
            _cachedSettings = new AppSettings();
        }

        return _cachedSettings;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsPath, json);
            _cachedSettings = settings;
        }
        catch (Exception)
        {
            // Log error here
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
            _cachedSettings = settings;
        }
        catch (Exception)
        {
            // Log error here
        }
    }

    public AppSettings Load()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        try
        {
            Directory.CreateDirectory(SettingsDir);

            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                _cachedSettings = new AppSettings();
                Save(_cachedSettings);
            }
        }
        catch (Exception)
        {
            _cachedSettings = new AppSettings();
        }

        return _cachedSettings;
    }

    public void ResetToDefaults()
    {
        _cachedSettings = new AppSettings();
        Save(_cachedSettings);
    }
}
