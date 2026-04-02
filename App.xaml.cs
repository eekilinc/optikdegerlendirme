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
        // Hata logu için dosya yolu
        var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_debug.log");
        
        try
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] App constructor STARTED\n");
            
            // Global exception handlers
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                var msg = $"[{DateTime.Now}] CRITICAL ERROR: {ex?.Message}\nSTACK: {ex?.StackTrace}\n\n";
                File.AppendAllText(logFile, msg);
                MessageBox.Show($"CRITICAL ERROR: {ex?.Message}\n\nSTACK: {ex?.StackTrace}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            
            DispatcherUnhandledException += (sender, e) =>
            {
                var msg = $"[{DateTime.Now}] DISPATCHER ERROR: {e.Exception.Message}\nSTACK: {e.Exception.StackTrace}\n\n";
                File.AppendAllText(logFile, msg);
                MessageBox.Show($"DISPATCHER ERROR: {e.Exception.Message}\n\nSTACK: {e.Exception.StackTrace}", "UI Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
            
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                var msg = $"[{DateTime.Now}] TASK ERROR: {e.Exception.Message}\n\n";
                File.AppendAllText(logFile, msg);
                MessageBox.Show($"TASK ERROR: {e.Exception.Message}", "Task Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.SetObserved();
            };
            
            File.AppendAllText(logFile, $"[{DateTime.Now}] App constructor COMPLETED\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] ERROR in constructor: {ex.Message}\n{ex.StackTrace}\n\n");
            throw;
        }
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
            
            // Manuel olarak MainWindow oluştur ve göster
            var mainWindow = new MainWindow();
            mainWindow.Show();
            
            base.OnStartup(e);
            
            // MainWindow oluşturulduktan sonra virtualization ayarla
            PerformanceOptimizer.EnableAdvancedVirtualization();
        }
        catch (Exception ex)
        {
            var errorMsg = $"Uygulama başlatılırken HATA:\n\n{ex.Message}\n\n";
            if (ex.InnerException != null)
                errorMsg += $"INNER: {ex.InnerException.Message}\n\n";
            errorMsg += $"STACK:\n{ex.StackTrace}";
            
            MessageBox.Show(errorMsg, "KRİTİK HATA", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Hata logunu dosyaya kaydet
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");
                File.WriteAllText(logPath, $"[{DateTime.Now}] {errorMsg}\n\n{ex}");
            }
            catch { }
            
            // Uygulamayı kapat
            Shutdown(1);
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

