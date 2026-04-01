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
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Debug log oluştur
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OptikDegerlendirme", "debug.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            
            using (StreamWriter writer = new StreamWriter(logPath, true))
            {
                writer.WriteLine($"[{DateTime.Now}] Uygulama başlatılıyor...");
                writer.WriteLine($"[{DateTime.Now}] Performans optimizasyonları başlıyor...");
            }
            
            // Performans optimizasyonları
            PerformanceOptimizer.OptimizeMemory();
            PerformanceOptimizer.OptimizeUI();
            
            using (StreamWriter writer = new StreamWriter(logPath, true))
            {
                writer.WriteLine($"[{DateTime.Now}] Performans optimizasyonları tamamlandı...");
                writer.WriteLine($"[{DateTime.Now}] MainWindow oluşturuluyor...");
            }
            
            base.OnStartup(e);
            
            // MainWindow oluşturulduktan sonra virtualization ayarla
            PerformanceOptimizer.EnableAdvancedVirtualization();
            
            using (StreamWriter writer = new StreamWriter(logPath, true))
            {
                writer.WriteLine($"[{DateTime.Now}] Uygulama başarıyla başlatıldı!");
            }
        }
        catch (Exception ex)
        {
            // Debug log'a hata yaz
            try
            {
                string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OptikDegerlendirme", "debug.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                
                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    writer.WriteLine($"[{DateTime.Now}] HATA: {ex.Message}");
                    writer.WriteLine($"[{DateTime.Now}] STACK TRACE: {ex.StackTrace}");
                }
            }
            catch { }
            
            MessageBox.Show($"Uygulama başlatılırken hata: {ex.Message}\n\nDetaylar için debug.log dosyasını kontrol edin:\n%AppData%\\OptikDegerlendirme\\debug.log", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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

