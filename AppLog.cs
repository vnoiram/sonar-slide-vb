using System;
using System.IO;

namespace SonarSlideVB;

internal static class AppLog
{
    private const long MaxLogBytes = 1024 * 1024;

    public static string LogPath => Path.Combine(AppConfig.ConfigDirectory, "app.log");
    private static string BackupLogPath => LogPath + ".1";

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Error(string message, Exception ex)
    {
        Write("ERROR", $"{message}: {ex}");
    }

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(AppConfig.ConfigDirectory);
            RotateIfNeeded();
            File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never break audio control.
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(LogPath) || new FileInfo(LogPath).Length <= MaxLogBytes)
        {
            return;
        }

        if (File.Exists(BackupLogPath))
        {
            File.Delete(BackupLogPath);
        }

        File.Move(LogPath, BackupLogPath);
    }
}
