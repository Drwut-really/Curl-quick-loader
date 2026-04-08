using System.Runtime.CompilerServices;

namespace CurlQuickLoader.Services;

public static class Logger
{
    private static string? _logFile;
    private static readonly object _lock = new();

    static Logger()
    {
        try
        {
            string baseDir = GetBaseDir();
            string logsDir = Path.Combine(baseDir, "logs");
            Directory.CreateDirectory(logsDir);
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            _logFile = Path.Combine(logsDir, $"app-{date}.log");
            Write("INFO", $"Logger initialized. Base dir: {baseDir}", "Logger");
            Write("INFO", $"Process path: {Environment.ProcessPath}", "Logger");
            Write("INFO", $"OS: {Environment.OSVersion}", "Logger");
            Write("INFO", $".NET version: {Environment.Version}", "Logger");
        }
        catch
        {
            // Logging is best-effort — never crash the app
        }
    }

    public static void Info(string message, [CallerMemberName] string? caller = null)
        => Write("INFO", message, caller);

    public static void Warn(string message, [CallerMemberName] string? caller = null)
        => Write("WARN", message, caller);

    public static void Error(string message, Exception? ex = null, [CallerMemberName] string? caller = null)
        => Write("ERROR", ex != null ? $"{message}\n  {ex.GetType().Name}: {ex.Message}\n  Stack: {ex.StackTrace}" : message, caller);

    private static void Write(string level, string message, string? caller)
    {
        if (_logFile == null) return;
        try
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-5}] [{caller}] {message}";
            lock (_lock)
                File.AppendAllText(_logFile, line + Environment.NewLine);
        }
        catch { }
    }

    /// <summary>
    /// Returns the directory of the running executable, correctly handling
    /// single-file deployments where AppContext.BaseDirectory points to the
    /// temp extraction folder rather than the actual exe location.
    /// </summary>
    public static string GetBaseDir()
    {
        try
        {
            string? processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                string? dir = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrEmpty(dir))
                    return dir;
            }
        }
        catch { }
        return AppContext.BaseDirectory;
    }
}
