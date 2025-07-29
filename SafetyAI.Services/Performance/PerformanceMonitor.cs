using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SafetyAI.Services.Infrastructure;

namespace SafetyAI.Services.Performance
{
    public class PerformanceMonitor : IPerformanceMonitor, IDisposable
    {
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;
        private readonly ConcurrentQueue<PerformanceEvent> _events;
        private readonly System.Threading.Timer _reportingTimer;
        private bool _disposed = false;

        public PerformanceMonitor()
        {
            _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
            _events = new ConcurrentQueue<PerformanceEvent>();
            
            // Report metrics every 5 minutes
            _reportingTimer = new System.Threading.Timer(ReportMetrics, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public IDisposable StartOperation(string operationName)
        {
            return new OperationTimer(this, operationName);
        }

        public void RecordOperation(string operationName, TimeSpan duration, bool success = true)
        {
            try
            {
                var metric = _metrics.GetOrAdd(operationName, _ => new PerformanceMetric(operationName));
                metric.RecordOperation(duration, success);

                var performanceEvent = new PerformanceEvent
                {
                    OperationName = operationName,
                    Duration = duration,
                    Success = success,
                    Timestamp = DateTime.UtcNow
                };

                _events.Enqueue(performanceEvent);

                // Keep only last 1000 events to prevent memory issues
                while (_events.Count > 1000)
                {
                    _events.TryDequeue(out _);
                }

                // Log slow operations
                if (duration.TotalSeconds > 10) // Operations taking more than 10 seconds
                {
                    Logger.LogWarning($"Slow operation detected: {operationName} took {duration.TotalSeconds:F2} seconds", "Performance");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording performance metric: {ex.Message}", "PerformanceMonitor");
            }
        }

        public void RecordMemoryUsage(string context, long memoryBytes)
        {
            try
            {
                var memoryMB = memoryBytes / (1024.0 * 1024.0);
                Logger.LogInfo($"Memory usage in {context}: {memoryMB:F2} MB", "Performance");

                // Alert on high memory usage (over 500MB)
                if (memoryMB > 500)
                {
                    Logger.LogWarning($"High memory usage detected in {context}: {memoryMB:F2} MB", "Performance");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording memory usage: {ex.Message}", "PerformanceMonitor");
            }
        }

        public void RecordDatabaseQuery(string queryType, TimeSpan duration, int recordCount = 0)
        {
            try
            {
                var operationName = $"DB_{queryType}";
                RecordOperation(operationName, duration);

                // Log slow queries
                if (duration.TotalSeconds > 5)
                {
                    Logger.LogWarning($"Slow database query: {queryType} took {duration.TotalSeconds:F2} seconds, returned {recordCount} records", "Performance");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording database query metric: {ex.Message}", "PerformanceMonitor");
            }
        }

        public void RecordApiCall(string endpoint, TimeSpan duration, int statusCode)
        {
            try
            {
                var operationName = $"API_{endpoint}";
                var success = statusCode >= 200 && statusCode < 300;
                RecordOperation(operationName, duration, success);

                // Log API performance issues
                if (duration.TotalSeconds > 3)
                {
                    Logger.LogWarning($"Slow API call: {endpoint} took {duration.TotalSeconds:F2} seconds, status: {statusCode}", "Performance");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error recording API call metric: {ex.Message}", "PerformanceMonitor");
            }
        }

        public PerformanceReport GetPerformanceReport()
        {
            try
            {
                var report = new PerformanceReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    Metrics = _metrics.Values.Select(m => new PerformanceMetricSummary
                    {
                        OperationName = m.OperationName,
                        TotalOperations = m.TotalOperations,
                        SuccessfulOperations = m.SuccessfulOperations,
                        FailedOperations = m.FailedOperations,
                        AverageDuration = m.AverageDuration,
                        MinDuration = m.MinDuration,
                        MaxDuration = m.MaxDuration,
                        SuccessRate = m.SuccessRate
                    }).ToList(),
                    RecentEvents = _events.TakeLast(100).ToList(),
                    SystemMetrics = GetSystemMetrics()
                };

                return report;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error generating performance report: {ex.Message}", "PerformanceMonitor");
                return new PerformanceReport { GeneratedAt = DateTime.UtcNow };
            }
        }

        private SystemMetrics GetSystemMetrics()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                
                return new SystemMetrics
                {
                    WorkingSetMemory = process.WorkingSet64,
                    PrivateMemory = process.PrivateMemorySize64,
                    ProcessorTime = process.TotalProcessorTime,
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount
                };
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting system metrics: {ex.Message}", "PerformanceMonitor");
                return new SystemMetrics();
            }
        }

        private void ReportMetrics(object state)
        {
            try
            {
                var report = GetPerformanceReport();
                
                Logger.LogInfo($"Performance Report - Total Operations: {report.Metrics.Sum(m => m.TotalOperations)}", "Performance");
                
                // Report top 5 slowest operations
                var slowestOperations = report.Metrics
                    .OrderByDescending(m => m.AverageDuration.TotalMilliseconds)
                    .Take(5);

                foreach (var operation in slowestOperations)
                {
                    Logger.LogInfo($"Operation: {operation.OperationName}, Avg: {operation.AverageDuration.TotalMilliseconds:F2}ms, Count: {operation.TotalOperations}", "Performance");
                }

                // Report system metrics
                Logger.LogInfo($"System - Memory: {report.SystemMetrics.WorkingSetMemory / (1024 * 1024):F2}MB, Threads: {report.SystemMetrics.ThreadCount}", "Performance");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in performance reporting: {ex.Message}", "PerformanceMonitor");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reportingTimer?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private class OperationTimer : IDisposable
        {
            private readonly PerformanceMonitor _monitor;
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;
            private bool _disposed = false;

            public OperationTimer(PerformanceMonitor monitor, string operationName)
            {
                _monitor = monitor;
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _stopwatch.Stop();
                    _monitor.RecordOperation(_operationName, _stopwatch.Elapsed);
                    _disposed = true;
                }
            }
        }
    }

    public interface IPerformanceMonitor
    {
        IDisposable StartOperation(string operationName);
        void RecordOperation(string operationName, TimeSpan duration, bool success = true);
        void RecordMemoryUsage(string context, long memoryBytes);
        void RecordDatabaseQuery(string queryType, TimeSpan duration, int recordCount = 0);
        void RecordApiCall(string endpoint, TimeSpan duration, int statusCode);
        PerformanceReport GetPerformanceReport();
    }

    public class PerformanceMetric
    {
        private readonly object _lock = new object();
        private long _totalOperations;
        private long _successfulOperations;
        private long _totalDurationTicks;
        private long _minDurationTicks = long.MaxValue;
        private long _maxDurationTicks = long.MinValue;

        public string OperationName { get; }
        public long TotalOperations => _totalOperations;
        public long SuccessfulOperations => _successfulOperations;
        public long FailedOperations => _totalOperations - _successfulOperations;
        public TimeSpan AverageDuration => _totalOperations > 0 ? new TimeSpan(_totalDurationTicks / _totalOperations) : TimeSpan.Zero;
        public TimeSpan MinDuration => _minDurationTicks != long.MaxValue ? new TimeSpan(_minDurationTicks) : TimeSpan.Zero;
        public TimeSpan MaxDuration => _maxDurationTicks != long.MinValue ? new TimeSpan(_maxDurationTicks) : TimeSpan.Zero;
        public double SuccessRate => _totalOperations > 0 ? (double)_successfulOperations / _totalOperations * 100 : 0;

        public PerformanceMetric(string operationName)
        {
            OperationName = operationName;
        }

        public void RecordOperation(TimeSpan duration, bool success)
        {
            lock (_lock)
            {
                _totalOperations++;
                if (success)
                {
                    _successfulOperations++;
                }

                var ticks = duration.Ticks;
                _totalDurationTicks += ticks;
                
                if (ticks < _minDurationTicks)
                {
                    _minDurationTicks = ticks;
                }
                
                if (ticks > _maxDurationTicks)
                {
                    _maxDurationTicks = ticks;
                }
            }
        }
    }

    public class PerformanceEvent
    {
        public string OperationName { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public List<PerformanceMetricSummary> Metrics { get; set; } = new List<PerformanceMetricSummary>();
        public List<PerformanceEvent> RecentEvents { get; set; } = new List<PerformanceEvent>();
        public SystemMetrics SystemMetrics { get; set; } = new SystemMetrics();
    }

    public class PerformanceMetricSummary
    {
        public string OperationName { get; set; }
        public long TotalOperations { get; set; }
        public long SuccessfulOperations { get; set; }
        public long FailedOperations { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public double SuccessRate { get; set; }
    }

    public class SystemMetrics
    {
        public long WorkingSetMemory { get; set; }
        public long PrivateMemory { get; set; }
        public TimeSpan ProcessorTime { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
    }
}