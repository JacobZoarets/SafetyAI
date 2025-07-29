using System;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace SafetyAI.Services.Infrastructure
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logDirectory;

        static Logger()
        {
            _logDirectory = HttpContext.Current?.Server?.MapPath("~/App_Data/Logs") ?? 
                           Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public static void LogInfo(string message, string category = "General")
        {
            WriteLog("INFO", message, category);
        }

        public static void LogWarning(string message, string category = "General")
        {
            WriteLog("WARN", message, category);
        }

        public static void LogError(string message, string category = "General")
        {
            WriteLog("ERROR", message, category);
        }

        public static void LogError(Exception exception, string category = "General")
        {
            var message = $"{exception.Message}\nStack Trace: {exception.StackTrace}";
            if (exception.InnerException != null)
            {
                message += $"\nInner Exception: {exception.InnerException.Message}";
            }
            WriteLog("ERROR", message, category);
        }

        public static void LogDebug(string message, string category = "General")
        {
            if (Debugger.IsAttached)
            {
                WriteLog("DEBUG", message, category);
            }
        }

        public static void LogGeminiAPICall(string endpoint, TimeSpan duration, bool success, string errorMessage = null)
        {
            var message = $"Gemini API Call - Endpoint: {endpoint}, Duration: {duration.TotalMilliseconds}ms, Success: {success}";
            if (!success && !string.IsNullOrEmpty(errorMessage))
            {
                message += $", Error: {errorMessage}";
            }
            
            WriteLog(success ? "INFO" : "ERROR", message, "GeminiAPI");
        }

        public static void LogProcessingMetrics(string operation, TimeSpan duration, bool success, int? fileSize = null)
        {
            var message = $"Processing Metrics - Operation: {operation}, Duration: {duration.TotalMilliseconds}ms, Success: {success}";
            if (fileSize.HasValue)
            {
                message += $", FileSize: {fileSize.Value} bytes";
            }
            
            WriteLog("METRICS", message, "Performance");
        }

        private static void WriteLog(string level, string message, string category)
        {
            lock (_lock)
            {
                try
                {
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] [{level}] [{category}] {message}";
                    
                    // Write to file
                    var logFileName = $"SafetyAI_{DateTime.UtcNow:yyyyMMdd}.log";
                    var logFilePath = Path.Combine(_logDirectory, logFileName);
                    
                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                    
                    // Also write to debug output
                    Debug.WriteLine(logEntry);
                    
                    // Write to console if available
                    Console.WriteLine(logEntry);
                }
                catch (Exception ex)
                {
                    // Fallback to debug output if file logging fails
                    Debug.WriteLine($"Logging failed: {ex.Message}");
                    Debug.WriteLine($"Original message: [{level}] [{category}] {message}");
                }
            }
        }

        public static void CleanupOldLogs(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(_logDirectory, "SafetyAI_*.log");
                
                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                        LogInfo($"Deleted old log file: {Path.GetFileName(logFile)}", "Maintenance");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to cleanup old logs: {ex.Message}", "Maintenance");
            }
        }
    }

    public class LogContext : IDisposable
    {
        private readonly string _operation;
        private readonly Stopwatch _stopwatch;
        private bool _disposed = false;

        public LogContext(string operation)
        {
            _operation = operation;
            _stopwatch = Stopwatch.StartNew();
            Logger.LogInfo($"Starting operation: {operation}", "Operations");
        }

        public void LogProgress(string message)
        {
            Logger.LogInfo($"[{_operation}] {message} (Elapsed: {_stopwatch.ElapsedMilliseconds}ms)", "Operations");
        }

        public void LogWarning(string message)
        {
            Logger.LogWarning($"[{_operation}] {message} (Elapsed: {_stopwatch.ElapsedMilliseconds}ms)", "Operations");
        }

        public void LogError(string message)
        {
            Logger.LogError($"[{_operation}] {message} (Elapsed: {_stopwatch.ElapsedMilliseconds}ms)", "Operations");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                Logger.LogInfo($"Completed operation: {_operation} (Total time: {_stopwatch.ElapsedMilliseconds}ms)", "Operations");
                _disposed = true;
            }
        }
    }
}