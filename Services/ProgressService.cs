using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Progress reporting service for long-running operations
    /// </summary>
    public class ProgressService : INotifyPropertyChanged
    {
        private static ProgressService? _instance;
        public static ProgressService Instance => _instance ??= new ProgressService();

        private bool _isBusy;
        private string _statusMessage = "";
        private int _currentProgress;
        private int _maximumProgress = 100;
        private string _operationTitle = "";
        private CancellationTokenSource? _cancellationTokenSource;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanCancel));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int CurrentProgress
        {
            get => _currentProgress;
            private set
            {
                _currentProgress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }

        public int MaximumProgress
        {
            get => _maximumProgress;
            private set
            {
                _maximumProgress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }

        public string OperationTitle
        {
            get => _operationTitle;
            private set
            {
                _operationTitle = value;
                OnPropertyChanged();
            }
        }

        public double ProgressPercentage => MaximumProgress > 0 
            ? (double)CurrentProgress / MaximumProgress * 100 
            : 0;

        public bool CanCancel => IsBusy && _cancellationTokenSource != null;

        public CancellationToken? CancellationToken => _cancellationTokenSource?.Token;

        public event PropertyChangedEventHandler? PropertyChanged;

        private ProgressService() { }

        public void StartOperation(string title, string initialMessage, int maxProgress = 100, bool allowCancel = true)
        {
            OperationTitle = title;
            StatusMessage = initialMessage;
            MaximumProgress = maxProgress;
            CurrentProgress = 0;
            _cancellationTokenSource = allowCancel ? new CancellationTokenSource() : null;
            IsBusy = true;
        }

        public void UpdateProgress(int current, string? message = null)
        {
            if (!IsBusy) return;
            
            CurrentProgress = Math.Min(current, MaximumProgress);
            if (message != null)
                StatusMessage = message;
        }

        public void IncrementProgress(int amount = 1, string? message = null)
        {
            UpdateProgress(CurrentProgress + amount, message);
        }

        public void UpdateStatus(string message)
        {
            if (!IsBusy) return;
            StatusMessage = message;
        }

        public void CompleteOperation()
        {
            CurrentProgress = MaximumProgress;
            IsBusy = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
        }

        public void Reset()
        {
            IsBusy = false;
            CurrentProgress = 0;
            MaximumProgress = 100;
            StatusMessage = "";
            OperationTitle = "";
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        /// <summary>
        /// Executes an async operation with progress reporting
        /// </summary>
        public async Task<T> ExecuteWithProgress<T>(
            string title,
            Func<IProgress<(int progress, string message)>, CancellationToken, Task<T>> operation,
            bool allowCancel = true)
        {
            StartOperation(title, "Başlatılıyor...", 100, allowCancel);
            
            try
            {
                var progress = new Progress<(int progress, string message)>(update =>
                {
                    UpdateProgress(update.progress, update.message);
                });

                var result = await operation(progress, _cancellationTokenSource?.Token ?? default);
                CompleteOperation();
                return result;
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("İşlem iptal edildi");
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Hata: {ex.Message}");
                throw;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
