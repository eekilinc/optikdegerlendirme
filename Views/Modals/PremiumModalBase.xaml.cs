using System.Windows;
using System.Windows.Controls;

namespace OptikFormApp.Views.Modals
{
    public partial class PremiumModalBase : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(PremiumModalBase), 
                new PropertyMetadata("Modal Başlığı"));

        public static new readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(object), typeof(PremiumModalBase), 
                new PropertyMetadata(null));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public new object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public event RoutedEventHandler? Close;
        public event RoutedEventHandler? Cancel;
        public event RoutedEventHandler? Confirm;

        public PremiumModalBase()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, e);
            CloseModal();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancel?.Invoke(this, e);
            CloseModal();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Confirm?.Invoke(this, e);
            CloseModal();
        }

        private async void CloseModal()
        {
            // Fade out animation
            var fadeOut = (System.Windows.Media.Animation.Storyboard)Resources["ModalFadeOut"];
            var slideOut = (System.Windows.Media.Animation.Storyboard)Resources["ModalSlideOut"];
            
            fadeOut.Begin();
            slideOut.Begin();
            
            await System.Threading.Tasks.Task.Delay(300);
            
            // Remove from parent
            if (Parent is Grid parentGrid)
            {
                parentGrid.Children.Remove(this);
            }
        }

        public void ShowModal(Grid container)
        {
            container.Children.Add(this);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            if (e.Property == TitleProperty)
            {
                TitleText.Text = (string)e.NewValue;
            }
            else if (e.Property == ContentProperty)
            {
                ModalContent.Content = e.NewValue;
            }
        }
    }
}
