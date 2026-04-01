using System;
using System.Windows;
using System.Windows.Controls;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Simple responsive design service
    /// </summary>
    public static class ResponsiveDesignService
    {
        public enum Breakpoint
        {
            Mobile,     // < 768px
            Tablet,     // 768px - 1024px
            Desktop     // > 1024px
        }

        public static Breakpoint CurrentBreakpoint { get; private set; } = Breakpoint.Desktop;

        static ResponsiveDesignService()
        {
            if (Application.Current?.MainWindow != null)
            {
                Application.Current.MainWindow.SizeChanged += OnWindowSizeChanged;
                UpdateBreakpoint(Application.Current.MainWindow.Width);
            }
        }

        private static void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBreakpoint(e.NewSize.Width);
        }

        private static void UpdateBreakpoint(double width)
        {
            var newBreakpoint = width switch
            {
                < 768 => Breakpoint.Mobile,
                < 1024 => Breakpoint.Tablet,
                _ => Breakpoint.Desktop
            };

            CurrentBreakpoint = newBreakpoint;
        }

        public static bool IsMobile => CurrentBreakpoint == Breakpoint.Mobile;
        public static bool IsTablet => CurrentBreakpoint == Breakpoint.Tablet;
        public static bool IsDesktop => CurrentBreakpoint == Breakpoint.Desktop;

        public static void ApplyResponsiveLayout(FrameworkElement element)
        {
            ApplyResponsiveStyles(element, CurrentBreakpoint);
        }

        private static void ApplyResponsiveStyles(FrameworkElement element, Breakpoint breakpoint)
        {
            switch (breakpoint)
            {
                case Breakpoint.Mobile:
                    ApplyMobileStyles(element);
                    break;
                case Breakpoint.Tablet:
                    ApplyTabletStyles(element);
                    break;
                case Breakpoint.Desktop:
                    ApplyDesktopStyles(element);
                    break;
            }
        }

        private static void ApplyMobileStyles(FrameworkElement element)
        {
            if (element is Button button)
            {
                button.MinHeight = 44;
                button.MinWidth = 44;
                button.Margin = new Thickness(4);
            }
        }

        private static void ApplyTabletStyles(FrameworkElement element)
        {
            if (element is Button button)
            {
                button.MinHeight = 40;
                button.MinWidth = 40;
                button.Margin = new Thickness(6);
            }
        }

        private static void ApplyDesktopStyles(FrameworkElement element)
        {
            if (element is Button button)
            {
                button.MinHeight = 32;
                button.MinWidth = 32;
                button.Margin = new Thickness(8);
            }
        }
    }
}
