using System.IO;

namespace Core.Services.Logging;

public static class FileLogger
{
    private static readonly object _lock = new();
    private static readonly string _logPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, 
        "Logs", 
        $"log_{DateTime.Now:yyyyMMdd}.txt"
    );

    static FileLogger()
    {
        var logDir = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
    }

    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
                File.AppendAllText(_logPath, logEntry + Environment.NewLine);
            }
        }
        catch
        {
        }
    }
}
