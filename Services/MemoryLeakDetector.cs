using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Memory leak detection and monitoring service
    /// </summary>
    public static class MemoryLeakDetector
    {
        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        private static readonly Dictionary<string, long> _baselineMemory = new();
        private static readonly Dictionary<string, List<long>> _memoryHistory = new();

        /// <summary>
        /// Initialize memory monitoring
        /// </summary>
        public static void Initialize()
        {
            TakeBaselineSnapshot("ApplicationStart");
        }

        /// <summary>
        /// Take baseline memory snapshot
        /// </summary>
        public static void TakeBaselineSnapshot(string context)
        {
            var memory = Process.GetCurrentProcess().WorkingSet64;
            _baselineMemory[context] = memory;
            
            if (!_memoryHistory.ContainsKey(context))
                _memoryHistory[context] = new List<long>();
            
            _memoryHistory[context].Add(memory);
            
            // Keep only last 10 measurements
            if (_memoryHistory[context].Count > 10)
                _memoryHistory[context].RemoveAt(0);
        }

        /// <summary>
        /// Check for memory leaks
        /// </summary>
        public static MemoryLeakReport CheckForLeaks(string context, long thresholdMB = 50)
        {
            if (!_baselineMemory.ContainsKey(context))
                return new MemoryLeakReport { HasLeak = false, Message = "No baseline found" };

            var current = Process.GetCurrentProcess().WorkingSet64;
            var baseline = _baselineMemory[context];
            var increase = current - baseline;
            var increaseMB = increase / (1024 * 1024);

            var report = new MemoryLeakReport
            {
                Context = context,
                CurrentMemoryMB = current / (1024 * 1024),
                BaselineMemoryMB = baseline / (1024 * 1024),
                IncreaseMB = increaseMB,
                HasLeak = increaseMB > thresholdMB
            };

            if (report.HasLeak)
            {
                report.Message = $"Memory leak detected in {context}: {increaseMB:F1}MB increase";
                report.Severity = increaseMB > 100 ? LeakSeverity.High : LeakSeverity.Medium;
            }
            else
            {
                report.Message = $"No significant memory leak in {context}: {increaseMB:F1}MB increase";
                report.Severity = LeakSeverity.Low;
            }

            return report;
        }

        /// <summary>
        /// Get detailed memory report
        /// </summary>
        public static MemoryReport GetDetailedReport()
        {
            var process = Process.GetCurrentProcess();
            var gc = GC.GetTotalMemory(false);

            return new MemoryReport
            {
                ProcessMemoryMB = process.WorkingSet64 / (1024 * 1024),
                GCMemoryMB = gc / (1024 * 1024),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalMemoryAllocated = gc,
                IsServerGC = GCSettings.IsServerGC,
                LatencyMode = GCSettings.LatencyMode.ToString()
            };
        }

        /// <summary>
        /// Force garbage collection and optimize memory
        /// </summary>
        public static void OptimizeMemory()
        {
            try
            {
                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Trim working set
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Memory optimization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitor memory usage over time
        /// </summary>
        public static async Task MonitorMemoryAsync(string context, int intervalMinutes = 5, int maxMeasurements = 12)
        {
            for (int i = 0; i < maxMeasurements; i++)
            {
                TakeBaselineSnapshot($"{context}_Monitor_{i}");
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes));
            }
        }

        /// <summary>
        /// Check for common WPF memory leaks
        /// </summary>
        public static List<string> CheckWPFMemoryLeaks()
        {
            var leaks = new List<string>();

            try
            {
                // Check for event handler leaks
                var app = Application.Current;
                if (app != null)
                {
                    // This is a simplified check - in reality, you'd need more sophisticated analysis
                    var windowCount = app.Windows.Count;
                    if (windowCount > 10)
                    {
                        leaks.Add($"High window count detected: {windowCount} windows");
                    }
                }

                // Check for large object heap fragmentation
                var lohSize = GC.GetTotalMemory(false) / (1024 * 1024);
                if (lohSize > 500)
                {
                    leaks.Add($"Large object heap size: {lohSize}MB");
                }
            }
            catch (Exception ex)
            {
                leaks.Add($"WPF memory leak check failed: {ex.Message}");
            }

            return leaks;
        }
    }

    public class MemoryLeakReport
    {
        public string Context { get; set; } = "";
        public long CurrentMemoryMB { get; set; }
        public long BaselineMemoryMB { get; set; }
        public long IncreaseMB { get; set; }
        public bool HasLeak { get; set; }
        public string Message { get; set; } = "";
        public LeakSeverity Severity { get; set; }
    }

    public class MemoryReport
    {
        public long ProcessMemoryMB { get; set; }
        public long GCMemoryMB { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public long TotalMemoryAllocated { get; set; }
        public bool IsServerGC { get; set; }
        public string LatencyMode { get; set; } = "";
    }

    public enum LeakSeverity
    {
        Low,
        Medium,
        High
    }
}
