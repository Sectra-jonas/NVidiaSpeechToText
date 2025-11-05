using System;
using System.IO;

namespace SpeechToTextTray.Utils
{
    /// <summary>
    /// Simple logger for debugging and error tracking
    /// </summary>
    public class Logger
    {
        private static readonly string LogDirectory;
        private static readonly string LogFilePath;
        private static readonly object LockObject = new object();

        static Logger()
        {
            LogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpeechToTextTray",
                "logs"
            );

            Directory.CreateDirectory(LogDirectory);

            LogFilePath = Path.Combine(
                LogDirectory,
                $"app_{DateTime.Now:yyyyMMdd}.log"
            );
        }

        public static void Info(string message)
        {
            Log("INFO", message);
        }

        public static void Warning(string message)
        {
            Log("WARN", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            Log("ERROR", message);
            if (ex != null)
            {
                Log("ERROR", $"  Exception: {ex.GetType().Name}");
                Log("ERROR", $"  Message: {ex.Message}");
                Log("ERROR", $"  StackTrace: {ex.StackTrace}");
            }
        }

        public static void Debug(string message)
        {
#if DEBUG
            Log("DEBUG", message);
#endif
        }

        private static void Log(string level, string message)
        {
            try
            {
                lock (LockObject)
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

                    // Write to file
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);

                    // Also write to debug output
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch
            {
                // Silently fail - logging should never crash the app
            }
        }

        public static void CleanupOldLogs(int daysToKeep = 7)
        {
            try
            {
                var files = Directory.GetFiles(LogDirectory, "app_*.log");
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up logs: {ex.Message}");
            }
        }
    }
}
