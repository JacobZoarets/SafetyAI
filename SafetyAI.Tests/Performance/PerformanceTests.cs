using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Services.Implementation;
using SafetyAI.Services.Performance;
using SafetyAI.Tests.Helpers;

namespace SafetyAI.Tests.Performance
{
    [TestClass]
    public class PerformanceTests
    {
        private PerformanceMonitor _performanceMonitor;
        private DocumentProcessor _documentProcessor;
        private SafetyAnalyzer _safetyAnalyzer;

        [TestInitialize]
        public void Setup()
        {
            _performanceMonitor = new PerformanceMonitor();
            var mockGeminiClient = TestDataHelper.CreateMockGeminiClient();
            _documentProcessor = new DocumentProcessor(mockGeminiClient, new FileValidator());
            _safetyAnalyzer = new SafetyAnalyzer(mockGeminiClient);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _performanceMonitor?.Dispose();
            _documentProcessor?.Dispose();
            _safetyAnalyzer?.Dispose();
        }

        [TestMethod]
        public async Task DocumentProcessing_Under10MB_ShouldCompleteWithin30Seconds()
        {
            // Arrange
            var testDocument = TestDataHelper.CreateTestPdfBytes("Large safety incident report with detailed information.", 5 * 1024 * 1024); // 5MB
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await _documentProcessor.ProcessDocumentAsync(testDocument, "large-document.pdf");

            // Assert
            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds < 30, $"Processing took {stopwatch.Elapsed.TotalSeconds} seconds, should be under 30");
            Assert.IsTrue(result.IsSuccess, "Large document processing should succeed");
        }

        [TestMethod]
        public async Task ConcurrentUsers_100Simultaneous_ShouldMaintainPerformance()
        {
            // Arrange
            const int concurrentUsers = 100;
            var tasks = new List<Task<bool>>();
            var testDocument = TestDataHelper.CreateTestPdfBytes("Concurrent user test document.");

            // Act
            var overallStopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < concurrentUsers; i++)
            {
                var userId = i;
                tasks.Add(Task.Run(async () =>
                {
                    using (var operation = _performanceMonitor.StartOperation($"ConcurrentUser_{userId}"))
                    {
                        var result = await _documentProcessor.ProcessDocumentAsync(testDocument, $"user-{userId}-document.pdf");
                        return result.IsSuccess;
                    }
                }));
            }

            var results = await Task.WhenAll(tasks);
            overallStopwatch.Stop();

            // Assert
            Assert.IsTrue(results.All(r => r), "All concurrent operations should succeed");
            Assert.IsTrue(overallStopwatch.Elapsed.TotalMinutes < 5, $"100 concurrent operations took {overallStopwatch.Elapsed.TotalMinutes} minutes, should be under 5");

            // Check performance metrics
            var report = _performanceMonitor.GetPerformanceReport();
            var concurrentMetrics = report.Metrics.Where(m => m.OperationName.StartsWith("ConcurrentUser_")).ToList();
            
            Assert.AreEqual(concurrentUsers, concurrentMetrics.Sum(m => m.TotalOperations), "Should have metrics for all concurrent operations");
            
            var averageResponseTime = concurrentMetrics.Average(m => m.AverageDuration.TotalSeconds);
            Assert.IsTrue(averageResponseTime < 3, $"Average response time was {averageResponseTime} seconds, should be under 3");
        }

        [TestMethod]
        public async Task MemoryUsage_UnderLoad_ShouldNotExceedLimits()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var testDocument = TestDataHelper.CreateTestPdfBytes("Memory usage test document.");

            // Act - Process multiple documents to test memory usage
            for (int i = 0; i < 50; i++)
            {
                var result = await _documentProcessor.ProcessDocumentAsync(testDocument, $"memory-test-{i}.pdf");
                Assert.IsTrue(result.IsSuccess);

                // Force garbage collection every 10 iterations
                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            // Assert
            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);

            _performanceMonitor.RecordMemoryUsage("MemoryUsageTest", finalMemory);
            
            // Memory increase should be reasonable (under 100MB for 50 documents)
            Assert.IsTrue(memoryIncreaseMB < 100, $"Memory increased by {memoryIncreaseMB:F2} MB, should be under 100 MB");
        }

        [TestMethod]
        public async Task DatabaseQueries_OptimizedPerformance_ShouldMeetTargets()
        {
            // Arrange
            using (var testDbContext = new TestDbContext())
            using (var unitOfWork = new Data.Repositories.UnitOfWork(testDbContext.Context))
            {
                // Seed test data
                var testReports = TestDataHelper.CreateMultipleTestReports(100);
                foreach (var report in testReports)
                {
                    await unitOfWork.SafetyReports.AddAsync(report);
                }
                await unitOfWork.SaveChangesAsync();

                // Act & Assert - Test various query performance
                await TestQueryPerformance("GetAllReports", async () =>
                {
                    var reports = await unitOfWork.SafetyReports.GetAllAsync();
                    return reports.Count();
                }, expectedMaxSeconds: 2);

                await TestQueryPerformance("GetRecentReports", async () =>
                {
                    var reports = await unitOfWork.SafetyReports.GetRecentReportsAsync(10);
                    return reports.Count();
                }, expectedMaxSeconds: 1);

                await TestQueryPerformance("GetReportsByDateRange", async () =>
                {
                    var reports = await unitOfWork.SafetyReports.GetReportsByDateRangeAsync(
                        DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
                    return reports.Count();
                }, expectedMaxSeconds: 2);
            }
        }

        [TestMethod]
        public void PerformanceMonitoring_SystemMetrics_ShouldTrackAccurately()
        {
            // Arrange & Act
            using (var operation = _performanceMonitor.StartOperation("TestOperation"))
            {
                // Simulate some work
                System.Threading.Thread.Sleep(100);
            }

            _performanceMonitor.RecordMemoryUsage("TestContext", GC.GetTotalMemory(false));
            _performanceMonitor.RecordDatabaseQuery("TestQuery", TimeSpan.FromMilliseconds(50), 10);
            _performanceMonitor.RecordApiCall("TestEndpoint", TimeSpan.FromMilliseconds(200), 200);

            // Assert
            var report = _performanceMonitor.GetPerformanceReport();
            
            Assert.IsTrue(report.Metrics.Any(), "Should have performance metrics");
            Assert.IsTrue(report.RecentEvents.Any(), "Should have recent events");
            Assert.IsNotNull(report.SystemMetrics, "Should have system metrics");
            
            var testOperationMetric = report.Metrics.FirstOrDefault(m => m.OperationName == "TestOperation");
            Assert.IsNotNull(testOperationMetric, "Should have TestOperation metric");
            Assert.AreEqual(1, testOperationMetric.TotalOperations, "Should have recorded one operation");
            Assert.IsTrue(testOperationMetric.AverageDuration.TotalMilliseconds >= 100, "Should have recorded operation duration");
        }

        [TestMethod]
        public async Task LoadTesting_SustainedLoad_ShouldMaintainStability()
        {
            // Arrange
            const int operationsPerSecond = 10;
            const int testDurationSeconds = 30;
            const int totalOperations = operationsPerSecond * testDurationSeconds;

            var testDocument = TestDataHelper.CreateTestPdfBytes("Load testing document.");
            var completedOperations = 0;
            var failedOperations = 0;
            var responseTimes = new List<double>();

            // Act
            var startTime = DateTime.UtcNow;
            var tasks = new List<Task>();

            for (int i = 0; i < totalOperations; i++)
            {
                var operationId = i;
                var task = Task.Run(async () =>
                {
                    var operationStart = DateTime.UtcNow;
                    try
                    {
                        var result = await _documentProcessor.ProcessDocumentAsync(testDocument, $"load-test-{operationId}.pdf");
                        if (result.IsSuccess)
                        {
                            System.Threading.Interlocked.Increment(ref completedOperations);
                        }
                        else
                        {
                            System.Threading.Interlocked.Increment(ref failedOperations);
                        }
                    }
                    catch
                    {
                        System.Threading.Interlocked.Increment(ref failedOperations);
                    }
                    finally
                    {
                        var responseTime = (DateTime.UtcNow - operationStart).TotalMilliseconds;
                        lock (responseTimes)
                        {
                            responseTimes.Add(responseTime);
                        }
                    }
                });

                tasks.Add(task);

                // Throttle to maintain operations per second
                if (i % operationsPerSecond == 0 && i > 0)
                {
                    await Task.Delay(1000);
                }
            }

            await Task.WhenAll(tasks);
            var totalTime = DateTime.UtcNow - startTime;

            // Assert
            var successRate = (double)completedOperations / totalOperations * 100;
            var averageResponseTime = responseTimes.Average();
            var maxResponseTime = responseTimes.Max();

            Assert.IsTrue(successRate >= 95, $"Success rate was {successRate:F2}%, should be at least 95%");
            Assert.IsTrue(averageResponseTime < 3000, $"Average response time was {averageResponseTime:F2}ms, should be under 3000ms");
            Assert.IsTrue(maxResponseTime < 10000, $"Max response time was {maxResponseTime:F2}ms, should be under 10000ms");
            
            Console.WriteLine($"Load Test Results:");
            Console.WriteLine($"Total Operations: {totalOperations}");
            Console.WriteLine($"Completed: {completedOperations}");
            Console.WriteLine($"Failed: {failedOperations}");
            Console.WriteLine($"Success Rate: {successRate:F2}%");
            Console.WriteLine($"Average Response Time: {averageResponseTime:F2}ms");
            Console.WriteLine($"Max Response Time: {maxResponseTime:F2}ms");
            Console.WriteLine($"Total Duration: {totalTime.TotalSeconds:F2}s");
        }

        private async Task TestQueryPerformance<T>(string queryName, Func<Task<T>> queryFunc, double expectedMaxSeconds)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var result = await queryFunc();
            
            stopwatch.Stop();
            _performanceMonitor.RecordDatabaseQuery(queryName, stopwatch.Elapsed);
            
            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds < expectedMaxSeconds, 
                $"{queryName} took {stopwatch.Elapsed.TotalSeconds:F2} seconds, should be under {expectedMaxSeconds}");
        }
    }
}