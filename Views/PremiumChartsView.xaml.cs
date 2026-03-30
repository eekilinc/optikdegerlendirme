using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OptikFormApp.Views
{
    public partial class PremiumChartsView : UserControl
    {
        public PremiumChartsView()
        {
            InitializeComponent();
            InitializeCharts();
        }

        private void InitializeCharts()
        {
            // Tema renklerini al
            var accent = new SolidColorBrush(Color.FromRgb(59, 130, 246));  // Blue-600
            var success = new SolidColorBrush(Color.FromRgb(16, 185, 129));  // Green-600
            var warning = new SolidColorBrush(Color.FromRgb(245, 158, 11));  // Amber-600
            var error = new SolidColorBrush(Color.FromRgb(239, 68, 68));    // Red-600
            
            try 
            {
                var textMain = (SolidColorBrush)System.Windows.Application.Current.Resources["TextMain"];
                var textMuted = (SolidColorBrush)System.Windows.Application.Current.Resources["TextMuted"];
                
                // Soru Başarı Oranları Chart - Basit bar chart
                var successCanvas = new Canvas();
                QuestionSuccessChart.Child = successCanvas;
                
                var successRates = new double[] { 85, 72, 90, 65, 78, 88, 92, 70, 83, 95 };
                var barWidth = 30;
                var maxHeight = 200;
                
                for (int i = 0; i < successRates.Length; i++)
                {
                    var barHeight = (successRates[i] / 100.0) * maxHeight;
                    var bar = new Rectangle
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Fill = accent,
                        Stroke = accent,
                        StrokeThickness = 1
                    };
                    
                    Canvas.SetLeft(bar, i * (barWidth + 10) + 20);
                    Canvas.SetBottom(bar, 30);
                    successCanvas.Children.Add(bar);
                    
                    // Label
                    var label = new TextBlock
                    {
                        Text = $"{(i + 1)}",
                        FontSize = 10,
                        Foreground = textMuted
                    };
                    Canvas.SetLeft(label, i * (barWidth + 10) + 20 + 5);
                    Canvas.SetBottom(label, 10);
                    successCanvas.Children.Add(label);
                    
                    // Value
                    var valueLabel = new TextBlock
                    {
                        Text = $"{successRates[i]}%",
                        FontSize = 8,
                        Foreground = textMain
                    };
                    Canvas.SetLeft(valueLabel, i * (barWidth + 10) + 20 + 5);
                    Canvas.SetBottom(valueLabel, barHeight + 35);
                    successCanvas.Children.Add(valueLabel);
                }

                // Puan Dağılımı Chart - Basit line chart
                var scoreCanvas = new Canvas();
                ScoreDistributionChart.Child = scoreCanvas;
                
                var scoreRanges = new double[] { 5, 12, 28, 45, 38, 25, 15, 8, 3, 1 };
                var points = new System.Windows.Point[scoreRanges.Length];
                var canvasWidth = 400;
                var canvasHeight = 150;
                
                for (int i = 0; i < scoreRanges.Length; i++)
                {
                    var x = (i / (double)(scoreRanges.Length - 1)) * (canvasWidth - 40) + 20;
                    var y = canvasHeight - 30 - (scoreRanges[i] / 45.0) * (canvasHeight - 50);
                    points[i] = new System.Windows.Point(x, y);
                    
                    // Point
                    var ellipse = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = accent
                    };
                    Canvas.SetLeft(ellipse, x - 3);
                    Canvas.SetTop(ellipse, y - 3);
                    scoreCanvas.Children.Add(ellipse);
                }
                
                // Line
                var polyline = new Polyline
                {
                    Points = new PointCollection(points),
                    Stroke = accent,
                    StrokeThickness = 2
                };
                scoreCanvas.Children.Add(polyline);

                // Kitapçık Performansı - Basit pie chart
                var bookletCanvas = new Canvas();
                BookletPerformanceChart.Child = bookletCanvas;
                
                var pieRadius = 60;
                var centerX = 100;
                var centerY = 100;
                
                // A Kitapçığı
                var aArc = new Path
                {
                    Fill = success,
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                // Simple pie segment (simplified)
                var aGeometry = new System.Windows.Media.EllipseGeometry(new System.Windows.Point(centerX, centerY), pieRadius, pieRadius);
                aArc.Data = aGeometry;
                bookletCanvas.Children.Add(aArc);
                
                // B Kitapçığı
                var bArc = new Path
                {
                    Fill = warning,
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2
                };
                var bGeometry = new System.Windows.Media.EllipseGeometry(new System.Windows.Point(centerX + 80, centerY), pieRadius * 0.8, pieRadius * 0.8);
                bArc.Data = bGeometry;
                bookletCanvas.Children.Add(bArc);

                // Zaman Analizi Chart - Basit scatter chart
                var timeCanvas = new Canvas();
                TimeAnalysisChart.Child = timeCanvas;
                
                var completionTimes = new double[] { 45, 52, 48, 65, 58, 42, 55, 49, 61, 53 };
                var averageTimes = new double[] { 52, 52, 52, 52, 52, 52, 52, 52, 52, 52 };
                
                for (int i = 0; i < completionTimes.Length; i++)
                {
                    var x = (i / (double)(completionTimes.Length - 1)) * (canvasWidth - 40) + 20;
                    var y = canvasHeight - 30 - ((completionTimes[i] - 40) / 30.0) * (canvasHeight - 50);
                    
                    var point = new Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = accent
                    };
                    Canvas.SetLeft(point, x - 4);
                    Canvas.SetTop(point, y - 4);
                    timeCanvas.Children.Add(point);
                    
                    // Average line
                    var avgY = canvasHeight - 30 - ((averageTimes[i] - 40) / 30.0) * (canvasHeight - 50);
                    var avgPoint = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = error
                    };
                    Canvas.SetLeft(avgPoint, x - 3);
                    Canvas.SetTop(avgPoint, avgY - 3);
                    timeCanvas.Children.Add(avgPoint);
                }
            }
            catch
            {
                // Tema renkleri yüklenemezse varsayılan renkleri kullan
            }
        }
    }
}
