using System.Configuration;
using System.Data;
using System.Windows;
using OptikFormApp.Services;
using System.Runtime.Versioning;

namespace OptikFormApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
[SupportedOSPlatform("windows")]
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Performans optimizasyonları
            PerformanceOptimizer.OptimizeMemory();
            PerformanceOptimizer.OptimizeUI();
            PerformanceOptimizer.EnableAdvancedVirtualization();
            
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Uygulama başlatılırken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            // Cleanup
            PerformanceOptimizer.OptimizeMemory();
            base.OnExit(e);
        }
        catch (Exception ex)
        {
            // Exit'te hata olursa sessizce geç
            System.Diagnostics.Debug.WriteLine($"Exit error: {ex.Message}");
        }
    }
}

