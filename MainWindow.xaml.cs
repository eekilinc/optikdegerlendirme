using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OptikFormApp.ViewModels;
using System.Linq;

namespace OptikFormApp;

public partial class MainWindow : Window
{
    private Border? _dropOverlay;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    // Drag-Drop Event Handlers
    private void MainWindow_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Any(f => System.IO.Path.GetExtension(f).ToLower() == ".txt"))
            {
                e.Effects = DragDropEffects.Copy;
                ShowDropOverlay();
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void MainWindow_DragLeave(object sender, DragEventArgs e)
    {
        HideDropOverlay();
        e.Handled = true;
    }

    private async void MainWindow_Drop(object sender, DragEventArgs e)
    {
        HideDropOverlay();
        
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var txtFile = files.FirstOrDefault(f => System.IO.Path.GetExtension(f).ToLower() == ".txt");
            
            if (txtFile != null && DataContext is MainViewModel vm)
            {
                await vm.LoadDroppedFileAsync(txtFile);
            }
        }
        e.Handled = true;
    }

    private void ShowDropOverlay()
    {
        if (_dropOverlay != null) return;

        _dropOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 59, 130, 246)), // Blue with opacity
            BorderBrush = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(16),
            Margin = new Thickness(20),
            Opacity = 0,
            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = "📄",
                        FontSize = 64,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 20)
                    },
                    new TextBlock
                    {
                        Text = "Dosyayı Bırak",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Colors.White),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "Optik form dosyasını (.txt) buraya bırakın",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(200, 220, 255)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 0)
                    }
                }
            }
        };
        Panel.SetZIndex(_dropOverlay, 3000);

        // Add to main grid
        if (Content is Grid mainGrid)
        {
            mainGrid.Children.Add(_dropOverlay);
            
            // Animate in
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            _dropOverlay.BeginAnimation(OpacityProperty, fadeIn);
        }
    }

    private void HideDropOverlay()
    {
        if (_dropOverlay == null) return;

        // Animate out
        var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(150)
        };
        
        fadeOut.Completed += (s, e) =>
        {
            if (_dropOverlay != null && Content is Grid mainGrid)
            {
                mainGrid.Children.Remove(_dropOverlay);
                _dropOverlay = null;
            }
        };
        
        _dropOverlay.BeginAnimation(OpacityProperty, fadeOut);
    }
}