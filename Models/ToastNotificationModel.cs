using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace OptikFormApp.Models
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class ToastNotificationModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ToastType Type { get; set; } = ToastType.Info;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(4);
        public bool IsPersistent { get; set; } = false;
        public double Progress { get; set; } = 100;
        
        public Brush BackgroundBrush => Type switch
        {
            ToastType.Success => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            ToastType.Warning => new SolidColorBrush(Color.FromRgb(251, 191, 36)),
            ToastType.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            _ => new SolidColorBrush(Color.FromRgb(59, 130, 246))
        };
        
        public Brush IconBrush => Type switch
        {
            ToastType.Success => new SolidColorBrush(Color.FromRgb(20, 83, 45)),
            ToastType.Warning => new SolidColorBrush(Color.FromRgb(120, 53, 15)),
            ToastType.Error => new SolidColorBrush(Color.FromRgb(127, 29, 29)),
            _ => new SolidColorBrush(Color.FromRgb(30, 58, 138))
        };
        
        public string IconPath => Type switch
        {
            ToastType.Success => "M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z",
            ToastType.Warning => "M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z",
            ToastType.Error => "M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z",
            _ => "M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
        };
    }
}
