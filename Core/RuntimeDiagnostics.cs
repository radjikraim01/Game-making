using System.Text;

namespace DungeonEscape.Core;

public static class RuntimeDiagnostics
{
    public const string LogDirEnvVar = "DUNGEON_ESCAPE_LOG_DIR";

    private static readonly object LogLock = new();

    public static string GetLogDirectory()
    {
        var fromEnv = Environment.GetEnvironmentVariable(LogDirEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv.Trim();
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DungeonEscapeRaylib",
            "logs");
    }

    public static string GetCurrentLogPath()
    {
        return Path.Combine(GetLogDirectory(), $"runtime-{DateTime.UtcNow:yyyyMMdd}.log");
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Warn(string message)
    {
        Write("WARN", message);
    }

    public static void Error(string message, Exception ex, string? context = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine(message);
        if (!string.IsNullOrWhiteSpace(context))
        {
            builder.AppendLine($"Context: {context}");
        }

        builder.AppendLine($"Exception: {ex.GetType().Name}: {ex.Message}");
        builder.AppendLine(ex.StackTrace ?? "(no stack trace)");
        Write("ERROR", builder.ToString().TrimEnd());
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (LogLock)
            {
                var logDir = GetLogDirectory();
                Directory.CreateDirectory(logDir);
                var line = $"[{DateTime.UtcNow:O}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(GetCurrentLogPath(), line, Encoding.UTF8);
            }
        }
        catch
        {
            // Diagnostics must never crash the game.
        }
    }
}

