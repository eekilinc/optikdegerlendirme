using OptikFormApp.Core.Models;
using OptikFormApp.Core.Interfaces;
using System.Text.Json;
using System.IO;

namespace OptikFormApp.Core.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsPath;
    private AppSettings? _cachedSettings;

    public AppSettingsService()
    {
        // Program Files dizinine yazma izni olmayabileceği için AppData'ya kaydet
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OptikDegerlendirme");
        
        // Klasör yoksa oluştur
        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);
        
        _settingsPath = Path.Combine(appDataPath, "appsettings.json");
    }

    public AppSettingsService(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public AppSettings LoadSettings()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                _cachedSettings = new AppSettings();
                SaveSettings(_cachedSettings);
            }
        }
        catch (Exception)
        {
            _cachedSettings = new AppSettings();
        }

        return _cachedSettings;
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
            _cachedSettings = settings;
        }
        catch (Exception)
        {
            // Log error here
        }
    }

    public void ResetToDefaults()
    {
        _cachedSettings = new AppSettings();
        SaveSettings(_cachedSettings);
    }

    public void ClearCache()
    {
        _cachedSettings = null;
    }
}
