using System.Configuration;
using System.Data;
using System.Windows;
using OptikFormApp.Services;
using System.Runtime.Versioning;
using System.IO;

namespace OptikFormApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
[SupportedOSPlatform("windows")]
public partial class App : Application
{
    static App()
    {
        // Static constructor - gerekirse buraya kod eklenebilir
    }

    public App()
    {
        // Constructor - gerekirse buraya kod eklenebilir
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // InitializeComponent çağır
            InitializeComponent();
            
            // Performans optimizasyonları
            PerformanceOptimizer.OptimizeMemory();
            PerformanceOptimizer.OptimizeUI();
            
            base.OnStartup(e);
            
            // MainWindow oluşturulduktan sonra virtualization ayarla
            PerformanceOptimizer.EnableAdvancedVirtualization();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Uygulama başlatılırken hata: {ex.Message}\n\nINNER: {ex.InnerException?.Message}\n\nSTACK: {ex.StackTrace}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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

