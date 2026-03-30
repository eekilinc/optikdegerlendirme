using System.Configuration;
using System.Data;
using System.Windows;
using OptikFormApp.Services;

namespace OptikFormApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Performans optimizasyonları
        PerformanceOptimizer.OptimizeMemory();
        PerformanceOptimizer.OptimizeUI();
        PerformanceOptimizer.EnableAdvancedVirtualization();
        
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Cleanup
        PerformanceOptimizer.OptimizeMemory();
        base.OnExit(e);
    }
}

