using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace OptikFormApp.Views.Modals
{
    public partial class PremiumNotificationModal : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(PremiumNotificationModal), 
                new PropertyMetadata("Bildirim"));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(PremiumNotificationModal), 
                new PropertyMetadata("İşlem tamamlandı."));

        public static readonly DependencyProperty NotificationTypeProperty =
            DependencyProperty.Register("NotificationType", typeof(NotificationType), typeof(PremiumNotificationModal), 
                new PropertyMetadata(NotificationType.Success));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public NotificationType NotificationType
        {
            get => (NotificationType)GetValue(NotificationTypeProperty);
            set => SetValue(NotificationTypeProperty, value);
        }

        public event RoutedEventHandler? Action;
        public event RoutedEventHandler? Dismiss;

        private System.Threading.Timer? _autoCloseTimer;

        public PremiumNotificationModal()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void ShowNotification(Grid container, int autoCloseMs = 5000)
        {
            container.Children.Add(this);
            
            // Auto-close timer
            if (autoCloseMs > 0)
            {
                _autoCloseTimer = new System.Threading.Timer(state => 
                {
                    Dispatcher.Invoke(() => DismissNotification());
                }, null, autoCloseMs, Timeout.Infinite);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Dismiss?.Invoke(this, e);
            DismissNotification();
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            Action?.Invoke(this, e);
            DismissNotification();
        }

        private void DismissButton_Click(object sender, RoutedEventArgs e)
        {
            Dismiss?.Invoke(this, e);
            DismissNotification();
        }

        private async void DismissNotification()
        {
            _autoCloseTimer?.Dispose();
            
            // Slide out animation
            var slideOut = (Storyboard)Resources["NotificationSlideOut"];
            slideOut.Begin();
            
            await Task.Delay(400);
            
            // Remove from parent
            if (Parent is Grid parentGrid)
            {
                parentGrid.Children.Remove(this);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            if (e.Property == TitleProperty)
            {
                TitleText.Text = (string)e.NewValue;
            }
            else if (e.Property == MessageProperty)
            {
                MessageText.Text = (string)e.NewValue;
            }
            else if (e.Property == NotificationTypeProperty)
            {
                UpdateNotificationStyle((NotificationType)e.NewValue);
            }
        }

        private void UpdateNotificationStyle(NotificationType type)
        {
            var iconBorder = FindName("IconBorder") as Border;
            if (iconBorder == null) return;

            switch (type)
            {
                case NotificationType.Success:
                    iconBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
                    // Success icon would be set here
                    break;
                case NotificationType.Warning:
                    iconBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11));
                    // Warning icon would be set here
                    break;
                case NotificationType.Error:
                    iconBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
                    // Error icon would be set here
                    break;
                case NotificationType.Info:
                    iconBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                    // Info icon would be set here
                    break;
            }
        }
    }

    public enum NotificationType
    {
        Success,
        Warning,
        Error,
        Info
    }
}
