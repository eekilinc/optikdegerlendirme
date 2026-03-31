using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Runtime.Versioning;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Sistem performansını optimize etmek için yardımcı sınıf
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class PerformanceOptimizer
    {
        // Memory management
        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        /// <summary>
        /// Bellek kullanımını optimize eder
        /// </summary>
        public static void OptimizeMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Process working set'ini küçült
                Process.GetCurrentProcess().MinWorkingSet = new IntPtr(500000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Memory optimization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// UI render performansını optimize eder
        /// </summary>
        public static void OptimizeUI()
        {
            try
            {
                // Render mode ayarla
                RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

                // Text rendering optimization
                TextOptions.TextFormattingModeProperty.OverrideMetadata(
                    typeof(Window),
                    new FrameworkPropertyMetadata(TextFormattingMode.Display));

                TextOptions.TextRenderingModeProperty.OverrideMetadata(
                    typeof(Window),
                    new FrameworkPropertyMetadata(TextRenderingMode.Auto));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UI optimization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Large data set'leri için virtualization ayarları
        /// </summary>
        public static void EnableAdvancedVirtualization()
        {
            try
            {
                // Virtualization settings
                System.Windows.Controls.VirtualizingStackPanel.SetIsVirtualizing(
                    Application.Current.MainWindow, true);

                System.Windows.Controls.VirtualizingStackPanel.SetVirtualizationMode(
                    Application.Current.MainWindow, 
                    System.Windows.Controls.VirtualizationMode.Recycling);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Virtualization setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Performans metriklerini ölçer
        /// </summary>
        public static PerformanceMetrics GetPerformanceMetrics()
        {
            var process = Process.GetCurrentProcess();
            
            return new PerformanceMetrics
            {
                MemoryUsage = process.WorkingSet64 / 1024 / 1024, // MB
                GCMemory = GC.GetTotalMemory(false) / 1024 / 1024, // MB
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                CpuTime = process.TotalProcessorTime.TotalMilliseconds
            };
        }
    }

    public class PerformanceMetrics
    {
        public long MemoryUsage { get; set; }
        public long GCMemory { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public double CpuTime { get; set; }

        public override string ToString()
        {
            return $"Memory: {MemoryUsage}MB | GC: {GCMemory}MB | Threads: {ThreadCount} | Handles: {HandleCount}";
        }
    }
}
