using System;
using System.IO;
using System.Text.Json;
using OptikFormApp.Models;

namespace OptikFormApp.Services
{
    public class AppSettingsService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OptikFormApp");

        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* İlk çalışmada veya bozuk dosyada varsayılanları kullan */ }

            return new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* Sessizce yoksay */ }
        }
    }
}
