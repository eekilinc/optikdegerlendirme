using System;
using System.Windows;
using System.Windows.Media;

namespace OptikFormApp.Services
{
    /// <summary>
    /// Simple theme service for dark/light mode switching
    /// </summary>
    public static class ThemeService
    {
        public enum ThemeType
        {
            Light,
            Dark
        }

        private static ThemeType _currentTheme = ThemeType.Light;

        public static ThemeType CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ApplyTheme(value);
                }
            }
        }

        public static void ApplyTheme(ThemeType theme)
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            switch (theme)
            {
                case ThemeType.Dark:
                    ApplyDarkTheme(app.Resources);
                    break;
                case ThemeType.Light:
                    ApplyLightTheme(app.Resources);
                    break;
            }
        }

        private static void ApplyDarkTheme(ResourceDictionary resources)
        {
            resources["AppBg"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            resources["CardBg"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            resources["PrimaryText"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            resources["SecondaryText"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            resources["PrimaryAccent"] = new SolidColorBrush(Color.FromRgb(66, 165, 245));
            resources["SuccessColor"] = new SolidColorBrush(Color.FromRgb(34, 197, 94));
            resources["WarningColor"] = new SolidColorBrush(Color.FromRgb(234, 179, 8));
            resources["ErrorColor"] = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        }

        private static void ApplyLightTheme(ResourceDictionary resources)
        {
            resources["AppBg"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
            resources["CardBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            resources["PrimaryText"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            resources["SecondaryText"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            resources["PrimaryAccent"] = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            resources["SuccessColor"] = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            resources["WarningColor"] = new SolidColorBrush(Color.FromRgb(251, 146, 60));
            resources["ErrorColor"] = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        }

        public static void ToggleTheme()
        {
            CurrentTheme = CurrentTheme == ThemeType.Light ? ThemeType.Dark : ThemeType.Light;
        }
    }
}
