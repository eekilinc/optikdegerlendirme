using OptikFormApp.Core.Interfaces;

namespace OptikFormApp.Core.Services;

public class GlobalExceptionHandler
{
    private readonly ILoggingService _logger;
    private static GlobalExceptionHandler? _instance;
    private static readonly object _lock = new object();

    private GlobalExceptionHandler(ILoggingService logger)
    {
        _logger = logger;
    }

    public static void Initialize(ILoggingService logger)
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = new GlobalExceptionHandler(logger);
                AppDomain.CurrentDomain.UnhandledException += _instance.OnUnhandledException;
                TaskScheduler.UnobservedTaskException += _instance.OnUnobservedTaskException;
            }
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        if (exception != null)
        {
            _logger.LogError(exception, "Unhandled exception occurred. IsTerminating: {IsTerminating}", e.IsTerminating);
            
            // In a real application, you might want to:
            // 1. Show a user-friendly error message
            // 2. Create a crash report
            // 3. Attempt to save user data
            // 4. Restart the application gracefully
            
            if (e.IsTerminating)
            {
                // Application is about to terminate
                _logger.LogInformation("Application is terminating due to unhandled exception");
            }
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unobserved task exception occurred");
        
        // Mark the exception as observed to prevent process termination
        e.SetObserved();
    }

    public static void HandleException(Exception exception, string context = "Unknown")
    {
        if (_instance != null)
        {
            _instance._logger.LogError(exception, "Exception handled in context: {Context}", context);
        }
        else
        {
            // Fallback: Write to console if logger is not available
            Console.WriteLine($"[{DateTime.Now}] ERROR in {context}: {exception.Message}");
            Console.WriteLine(exception.StackTrace);
        }
    }

    public static void Shutdown()
    {
        lock (_lock)
        {
            if (_instance != null)
            {
                AppDomain.CurrentDomain.UnhandledException -= _instance.OnUnhandledException;
                TaskScheduler.UnobservedTaskException -= _instance.OnUnobservedTaskException;
                _instance = null;
            }
        }
    }
}
