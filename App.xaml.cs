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
        try
        {
            MessageBox.Show("STATIC CONSTRUCTOR - Step 0", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"STATIC HATA: {ex.Message}", "KRİTİK HATA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public App()
    {
        try
        {
            MessageBox.Show("CONSTRUCTOR - Step 0.5", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
            InitializeComponent();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CONSTRUCTOR HATA: {ex.Message}", "KRİTİK HATA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // En basit debug - MessageBox
            MessageBox.Show("Uygulama başlatılıyor... Step 1", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Performans optimizasyonları
            PerformanceOptimizer.OptimizeMemory();
            PerformanceOptimizer.OptimizeUI();
            
            MessageBox.Show("Performans optimizasyonları tamamlandı... Step 2", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
            
            base.OnStartup(e);
            
            // MainWindow oluşturulduktan sonra virtualization ayarla
            PerformanceOptimizer.EnableAdvancedVirtualization();
            
            MessageBox.Show("Uygulama başarıyla başlatıldı! Step 3", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"HATA: {ex.Message}\n\nINNER: {ex.InnerException?.Message}\n\nSTACK: {ex.StackTrace}", "KRİTİK HATA", MessageBoxButton.OK, MessageBoxImage.Error);
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

