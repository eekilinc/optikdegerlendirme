using OptikFormApp.Core.Interfaces;
using Serilog;
using System.IO;

namespace OptikFormApp.Core.Services;

public class LoggingService : ILoggingService
{
    private readonly ILogger _logger;
    private static readonly object _lock = new object();
    private static bool _isConfigured = false;

    public LoggingService()
    {
        if (!_isConfigured)
        {
            lock (_lock)
            {
                if (!_isConfigured)
                {
                    ConfigureLogging();
                    _isConfigured = true;
                }
            }
        }
        _logger = Log.ForContext<LoggingService>();
    }

    private static void ConfigureLogging()
    {
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logPath, "optik-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.Console()
            .CreateLogger();
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.Information(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.Warning(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.Error(exception, message, args);
    }

    public void LogError(string message, params object[] args)
    {
        _logger.Error(message, args);
    }

    public void LogDebug(string message, params object[] args)
    {
        _logger.Debug(message, args);
    }

    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}
