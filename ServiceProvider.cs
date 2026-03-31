using System;
using Microsoft.Extensions.DependencyInjection;
using OptikFormApp.Services;
using OptikFormApp.ViewModels;

namespace OptikFormApp
{
    /// <summary>
    /// Dependency Injection Container - Servisleri ve ViewModel'ları yönetir
    /// </summary>
    public static class ServiceProvider
    {
        private static IServiceProvider? _provider;
        private static readonly object _lock = new();

        public static IServiceProvider Instance
        {
            get
            {
                if (_provider == null)
                {
                    lock (_lock)
                    {
                        _provider ??= BuildServiceProvider();
                    }
                }
                return _provider;
            }
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // Singleton Services - Uygulama ömrü boyunca tek instance
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<AppSettingsService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<UndoRedoManager>(sp => new UndoRedoManager(50));

            // Transient Services - Her kullanımda yeni instance
            services.AddTransient<OpticalParserService>();
            services.AddTransient<ExcelExportService>();
            services.AddTransient<CsvExportService>();
            services.AddTransient<PdfReportService>();
            services.AddTransient<ValidationService>();

            // ViewModel - Her pencere için yeni instance
            services.AddTransient<MainViewModel>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Servis çözümleme (Resolve)
        /// </summary>
        public static T GetService<T>() where T : notnull
        {
            return Instance.GetRequiredService<T>();
        }

        /// <summary>
        /// Servis çözümleme (null dönebilir)
        /// </summary>
        public static T? GetServiceOrDefault<T>() where T : class
        {
            return Instance.GetService<T>();
        }
    }
}
