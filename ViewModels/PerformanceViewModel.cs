using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OptikFormApp.ViewModels
{
    /// <summary>
    /// Performans optimizasyonu için ViewModel
    /// Lazy loading ve virtualization destekler
    /// </summary>
    public class PerformanceViewModel : INotifyPropertyChanged
    {
        private readonly object _lockObject = new object();
        private bool _isLoading;
        private int _totalCount;
        private int _loadedCount;
        private string _performanceStatus = "Hazır";

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int LoadedCount
        {
            get => _loadedCount;
            set => SetProperty(ref _loadedCount, value);
        }

        public string PerformanceStatus
        {
            get => _performanceStatus;
            set => SetProperty(ref _performanceStatus, value);
        }

        // Virtualization için optimize edilmiş collection
        public ObservableCollection<object> VirtualizedItems { get; } = new();

        // Memory management
        public void ClearCache()
        {
            lock (_lockObject)
            {
                VirtualizedItems.Clear();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        // Lazy loading simulation
        public async Task LoadItemsAsync(int startIndex, int count)
        {
            await Task.Run(() =>
            {
                IsLoading = true;
                PerformanceStatus = $"Yükleniyor... {startIndex}-{startIndex + count}";

                // Simulate data loading
                for (int i = 0; i < count; i++)
                {
                    var item = new { Index = startIndex + i, Data = $"Item {startIndex + i}" };
                    
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        VirtualizedItems.Add(item);
                        LoadedCount++;
                    });
                }

                IsLoading = false;
                PerformanceStatus = $"Yüklendi: {LoadedCount}/{TotalCount}";
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
