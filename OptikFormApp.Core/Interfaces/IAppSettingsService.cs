using OptikFormApp.Core.Models;

namespace OptikFormApp.Core.Interfaces;

public interface IAppSettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
    void ResetToDefaults();
}
