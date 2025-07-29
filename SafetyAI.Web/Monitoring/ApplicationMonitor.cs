using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;
using SafetyAI.Services.Infrastructure;
using SafetyAI.Services.Performance;

namespace SafetyAI.Web.Monitoring
{
    public class ApplicationMonitor : IHttpModule
    {
        private static readonly PerformanceCounter _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private static readonly PerformanceCounter _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        private static readonly System.Threading.Timer _monitoringTimer;
        private static readonly Dictionary<string, int> _errorCounts = new Dictionary<string, int>();
        private static readonly object _lockObject = new object();

        static ApplicationMonitor()
        {
            // Start monitoring timer - runs every 5 minutes
            _monitoringTimer = new System.Threading.Timer(CollectSystemMetrics, null, 
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
            context.Error += OnError;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                // Store request start time
                context.Items["RequestStartTime"] = DateTime.UtcNow;
                
                // Log request details for monitoring
                var requestPath = context.Request.Path;
                var userAgent = context.Request.UserAgent ?? "Unknown";
                var ipAddress = GetClientIpAddress(context.Request);
                
                Logger.LogInfo($"Request started: {requestPath} from {ipAddress}", "Monitor");
            }
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            if (context != null && context.Items["RequestStartTime"] is DateTime startTime)
            {
                var duration = DateTime.UtcNow - startTime;
                var statusCode = context.Response.StatusCode;
                var requestPath = context.Request.Path;
                
                // Log slow requests
                if (duration.TotalSeconds > 5)
                {
                    Logger.LogWarning($"Slow request detected: {requestPath} took {duration.TotalSeconds:F2} seconds", "Monitor");
                }
                
                // Log error responses
                if (statusCode >= 400)
                {
                    Logger.LogWarning($"Error response: {statusCode} for {requestPath}", "Monitor");
                    
                    lock (_lockObject)
                    {
                        var errorKey = $"{statusCode}_{requestPath}";
                        _errorCounts[errorKey] = _errorCounts.GetValueOrDefault(errorKey, 0) + 1;
                    }
                }
                
                // Record performance metrics
                RecordRequestMetrics(requestPath, duration, statusCode);
            }
        }

        private void OnError(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                var exception = context.Server.GetLastError();
                if (exception != null)
                {
                    var requestPath = context.Request.Path;
                    var ipAddress = GetClientIpAddress(context.Request);
                    
                    Logger.LogError($"Unhandled exception in {requestPath} from {ipAddress}: {exception.Message}", "Monitor");
                    
                    // Record error metrics
                    lock (_lockObject)
                    {
                        var errorKey = $"Exception_{exception.GetType().Name}";
                        _errorCounts[errorKey] = _errorCounts.GetValueOrDefault(errorKey, 0) + 1;
                    }
                    
                    // Send alert for critical errors
                    if (IsCriticalError(exception))
                    {
                        Task.Run(() => SendCriticalErrorAlert(exception, requestPath, ipAddress));
                    }
                }
            }
        }

        private static void CollectSystemMetrics(object state)
        {
            try
            {
                // Collect CPU usage
                var cpuUsage = _cpuCounter.NextValue();
                
                // Collect memory usage
                var availableMemoryMB = _memoryCounter.NextValue();
                
                // Get process-specific metrics
                var currentProcess = Process.GetCurrentProcess();
                var workingSetMB = currentProcess.WorkingSet64 / (1024 * 1024);
                var privateMemoryMB = currentProcess.PrivateMemorySize64 / (1024 * 1024);
                var threadCount = currentProcess.Threads.Count;
                var handleCount = currentProcess.HandleCount;
                
                // Log system metrics
                Logger.LogInfo($"System Metrics - CPU: {cpuUsage:F1}%, Available Memory: {availableMemoryMB:F0}MB, " +
                              $"Working Set: {workingSetMB:F0}MB, Private Memory: {privateMemoryMB:F0}MB, " +
                              $"Threads: {threadCount}, Handles: {handleCount}", "SystemMonitor");
                
                // Check for resource alerts
                if (cpuUsage > 80)
                {
                    Logger.LogWarning($"High CPU usage detected: {cpuUsage:F1}%", "SystemMonitor");
                }
                
                if (availableMemoryMB < 500)
                {
                    Logger.LogWarning($"Low available memory: {availableMemoryMB:F0}MB", "SystemMonitor");
                }
                
                if (workingSetMB > 1000)
                {
                    Logger.LogWarning($"High working set memory: {workingSetMB:F0}MB", "SystemMonitor");
                }
                
                // Report error counts
                lock (_lockObject)
                {
                    if (_errorCounts.Count > 0)
                    {
                        Logger.LogInfo($"Error counts in last period: {string.Join(", ", _errorCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}", "ErrorMonitor");
                        _errorCounts.Clear();
                    }
                }
                
                // Collect garbage collection metrics
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);
                var totalMemory = GC.GetTotalMemory(false) / (1024 * 1024);
                
                Logger.LogInfo($"GC Metrics - Gen0: {gen0Collections}, Gen1: {gen1Collections}, Gen2: {gen2Collections}, " +
                              $"Total Managed Memory: {totalMemory:F0}MB", "GCMonitor");
                
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error collecting system metrics: {ex.Message}", "SystemMonitor");
            }
        }

        private void RecordRequestMetrics(string requestPath, TimeSpan duration, int statusCode)
        {
            try
            {
                // This would typically send metrics to a monitoring system like Application Insights
                // For now, we'll just log significant metrics
                
                if (duration.TotalSeconds > 3)
                {
                    Logger.LogWarning($"Performance: {requestPath} - {duration.TotalMilliseconds:F0}ms - Status: {statusCode}", "Performance");
                }
                
                // Record API endpoint metrics
                if (requestPath.StartsWith("/api/"))
                {
                    Logger.LogInfo($"API Metrics: {requestPath} - {duration.TotalMilliseconds:F0}ms - Status: {statusCode}", "APIMetrics");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording request metrics: {ex.Message}", "Monitor");
            }
        }

        private string GetClientIpAddress(HttpRequest request)
        {
            try
            {
                // Check for forwarded IP first (load balancer scenarios)
                var forwardedFor = request.Headers["X-Forwarded-For"];
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    return forwardedFor.Split(',')[0].Trim();
                }
                
                var realIp = request.Headers["X-Real-IP"];
                if (!string.IsNullOrEmpty(realIp))
                {
                    return realIp;
                }
                
                return request.UserHostAddress ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private bool IsCriticalError(Exception exception)
        {
            // Define what constitutes a critical error
            return exception is OutOfMemoryException ||
                   exception is StackOverflowException ||
                   exception is System.Data.SqlClient.SqlException ||
                   exception.Message.Contains("database") ||
                   exception.Message.Contains("timeout");
        }

        private async Task SendCriticalErrorAlert(Exception exception, string requestPath, string ipAddress)
        {
            try
            {
                // This would typically send alerts via email, SMS, or monitoring service
                // For now, we'll just log a critical alert
                
                var alertMessage = $"CRITICAL ERROR ALERT\n" +
                                  $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                  $"Path: {requestPath}\n" +
                                  $"IP: {ipAddress}\n" +
                                  $"Exception: {exception.GetType().Name}\n" +
                                  $"Message: {exception.Message}\n" +
                                  $"Stack Trace: {exception.StackTrace}";
                
                Logger.LogError(alertMessage, "CriticalAlert");
                
                // In a real implementation, you would:
                // 1. Send email to administrators
                // 2. Send SMS alerts
                // 3. Post to monitoring service (e.g., PagerDuty, Slack)
                // 4. Create incident tickets
                
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to send critical error alert: {ex.Message}", "AlertSystem");
            }
        }

        public static Dictionary<string, object> GetCurrentMetrics()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                
                return new Dictionary<string, object>
                {
                    ["Timestamp"] = DateTime.UtcNow,
                    ["CPUUsage"] = _cpuCounter.NextValue(),
                    ["AvailableMemoryMB"] = _memoryCounter.NextValue(),
                    ["WorkingSetMB"] = currentProcess.WorkingSet64 / (1024 * 1024),
                    ["PrivateMemoryMB"] = currentProcess.PrivateMemorySize64 / (1024 * 1024),
                    ["ThreadCount"] = currentProcess.Threads.Count,
                    ["HandleCount"] = currentProcess.HandleCount,
                    ["Gen0Collections"] = GC.CollectionCount(0),
                    ["Gen1Collections"] = GC.CollectionCount(1),
                    ["Gen2Collections"] = GC.CollectionCount(2),
                    ["TotalManagedMemoryMB"] = GC.GetTotalMemory(false) / (1024 * 1024),
                    ["ErrorCounts"] = new Dictionary<string, int>(_errorCounts)
                };
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting current metrics: {ex.Message}", "Monitor");
                return new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["Timestamp"] = DateTime.UtcNow
                };
            }
        }

        public void Dispose()
        {
            try
            {
                _cpuCounter?.Dispose();
                _memoryCounter?.Dispose();
                _monitoringTimer?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error disposing ApplicationMonitor: {ex.Message}", "Monitor");
            }
        }
    }
}