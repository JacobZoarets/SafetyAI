using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SafetyAI.Data.Interfaces;
using SafetyAI.Models.DTOs;
using SafetyAI.Models.Entities;
using SafetyAI.Models.Enums;
using SafetyAI.Services.Infrastructure;
using SafetyAI.Services.Interfaces;

namespace SafetyAI.Services.Implementation
{
    public class AnalyticsService : IAnalyticsService, IDisposable
    {
        private readonly IUnitOfWork _unitOfWork;
        private bool _disposed = false;

        public AnalyticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<SafetyDashboardMetrics> GetDashboardMetricsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            using (var context = new LogContext("GetDashboardMetrics"))
            {
                try
                {
                    var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                    var end = endDate ?? DateTime.UtcNow;

                    context.LogProgress($"Calculating dashboard metrics for period {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");

                    var metrics = new SafetyDashboardMetrics
                    {
                        PeriodStart = start,
                        PeriodEnd = end,
                        GeneratedAt = DateTime.UtcNow
                    };

                    // Get all reports in the period
                    var reports = await _unitOfWork.SafetyReports.GetReportsByDateRangeAsync(start, end);
                    var reportsList = reports.ToList();

                    // Basic counts
                    metrics.TotalReports = reportsList.Count;
                    metrics.PendingReports = reportsList.Count(r => r.Status == ProcessingStatus.Pending);
                    metrics.CompletedReports = reportsList.Count(r => r.Status == ProcessingStatus.Completed);
                    metrics.FailedReports = reportsList.Count(r => r.Status == ProcessingStatus.Failed);

                    // Get analysis results for completed reports
                    var analysisResults = new List<AnalysisResult>();
                    foreach (var report in reportsList.Where(r => r.Status == ProcessingStatus.Completed))
                    {
                        var analysis = await _unitOfWork.AnalysisResults.FirstOrDefaultAsync(a => a.ReportId == report.Id);
                        if (analysis != null)
                        {
                            analysisResults.Add(analysis);
                        }
                    }

                    // Critical incidents
                    metrics.CriticalIncidents = analysisResults.Count(a => a.Severity == "Critical" || a.RiskScore >= 8);

                    // Average risk score
                    metrics.AverageRiskScore = analysisResults.Any() ? analysisResults.Average(a => a.RiskScore) : 0;

                    // Incident type distribution
                    metrics.IncidentTypeDistribution = analysisResults
                        .GroupBy(a => a.IncidentType ?? "Unknown")
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Severity distribution
                    metrics.SeverityDistribution = analysisResults
                        .GroupBy(a => a.Severity ?? "Unknown")
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Monthly trends (last 12 months)
                    metrics.MonthlyTrends = await CalculateMonthlyTrends();

                    // Top recommendations
                    var allRecommendations = await _unitOfWork.Recommendations.GetPendingRecommendationsAsync();
                    metrics.TopRecommendations = allRecommendations
                        .Take(5)
                        .Select(r => new SafetyRecommendation
                        {
                            Type = r.RecommendationType,
                            Description = r.Description,
                            Priority = r.Priority,
                            EstimatedCost = r.EstimatedCost,
                            ResponsibleRole = r.ResponsibleRole
                        })
                        .ToList();

                    context.LogProgress($"Dashboard metrics calculated: {metrics.TotalReports} reports, {metrics.CriticalIncidents} critical incidents");
                    return metrics;
                }
                catch (Exception ex)
                {
                    context.LogError($"Failed to calculate dashboard metrics: {ex.Message}");
                    Logger.LogError(ex, "AnalyticsService");
                    throw;
                }
            }
        }

        public async Task<SafetyTrendAnalysis> AnalyzeHistoricalTrendsAsync(int periodDays = 90)
        {
            using (var context = new LogContext("AnalyzeHistoricalTrends"))
            {
                try
                {
                    context.LogProgress($"Analyzing historical trends for {periodDays} days");

                    var endDate = DateTime.UtcNow;
                    var startDate = endDate.AddDays(-periodDays);

                    // Get all analysis results in the period
                    var analysisResults = await _unitOfWork.AnalysisResults.GetAnalysisByDateRangeAsync(startDate, endDate);
                    var analysisList = analysisResults.ToList();

                    // Convert to SafetyAnalysisResult DTOs
                    var safetyAnalyses = analysisList.Select(a => new SafetyAnalysisResult
                    {
                        IncidentType = ParseIncidentType(a.IncidentType),
                        Severity = ParseSeverityLevel(a.Severity),
                        RiskScore = a.RiskScore,
                        Summary = a.Summary,
                        AnalysisConfidence = (double)a.ConfidenceLevel
                    }).ToList();

                    // Mock trend analysis for development
                    var trendAnalysis = new SafetyTrendAnalysis
                    {
                        AnalysisDate = DateTime.UtcNow,
                        TotalIncidents = safetyAnalyses.Count,
                        AnalysisPeriodDays = periodDays,
                        AverageRiskScore = safetyAnalyses.Any() ? safetyAnalyses.Average(a => a.RiskScore) : 0,
                        RiskScoreTrend = "Stable",
                        IdentifiedPatterns = new List<string> { "Mock pattern analysis" },
                        TrendRecommendations = new List<SafetyRecommendation>
                        {
                            new SafetyRecommendation
                            {
                                Type = "Strategic",
                                Description = "Mock trend recommendation",
                                Priority = "Medium",
                                EstimatedCost = 1000,
                                EstimatedTimeHours = 10,
                                ResponsibleRole = "Safety Director"
                            }
                        }
                    };
                    
                    // Populate mock incident type trends
                    if (safetyAnalyses.Any())
                    {
                        trendAnalysis.IncidentTypeTrends = safetyAnalyses
                            .GroupBy(a => a.IncidentType)
                            .ToDictionary(g => g.Key, g => g.Count());
                        trendAnalysis.SeverityTrends = safetyAnalyses
                            .GroupBy(a => a.Severity)
                            .ToDictionary(g => g.Key, g => g.Count());
                    }

                    context.LogProgress($"Historical trend analysis completed for {safetyAnalyses.Count} incidents");
                    return trendAnalysis;
                }
                catch (Exception ex)
                {
                    context.LogError($"Historical trend analysis failed: {ex.Message}");
                    Logger.LogError(ex, "AnalyticsService");
                    throw;
                }
            }
        }

        public async Task<List<SafetyMetricsByLocation>> GetMetricsByLocationAsync()
        {
            using (var context = new LogContext("GetMetricsByLocation"))
            {
                try
                {
                    context.LogProgress("Calculating safety metrics by location");

                    // This would typically come from a location field in the database
                    // For now, we'll create sample data based on user information
                    var reports = await _unitOfWork.SafetyReports.GetAllAsync();
                    var reportsList = reports.ToList();

                    var locationMetrics = new List<SafetyMetricsByLocation>();

                    // Group by uploaded user as a proxy for location
                    var userGroups = reportsList.GroupBy(r => r.UploadedBy ?? "Unknown");

                    foreach (var userGroup in userGroups)
                    {
                        var userReports = userGroup.ToList();
                        var location = ExtractLocationFromUser(userGroup.Key);

                        var analysisResults = new List<AnalysisResult>();
                        foreach (var report in userReports)
                        {
                            var analysis = await _unitOfWork.AnalysisResults.FirstOrDefaultAsync(a => a.ReportId == report.Id);
                            if (analysis != null)
                            {
                                analysisResults.Add(analysis);
                            }
                        }

                        var metrics = new SafetyMetricsByLocation
                        {
                            Location = location,
                            TotalIncidents = userReports.Count,
                            CriticalIncidents = analysisResults.Count(a => a.Severity == "Critical"),
                            AverageRiskScore = analysisResults.Any() ? analysisResults.Average(a => a.RiskScore) : 0,
                            MostCommonIncidentType = analysisResults
                                .GroupBy(a => a.IncidentType)
                                .OrderByDescending(g => g.Count())
                                .FirstOrDefault()?.Key ?? "Unknown",
                            LastIncidentDate = userReports.Max(r => r.UploadedDate)
                        };

                        locationMetrics.Add(metrics);
                    }

                    context.LogProgress($"Location metrics calculated for {locationMetrics.Count} locations");
                    return locationMetrics.OrderByDescending(m => m.TotalIncidents).ToList();
                }
                catch (Exception ex)
                {
                    context.LogError($"Location metrics calculation failed: {ex.Message}");
                    Logger.LogError(ex, "AnalyticsService");
                    throw;
                }
            }
        }

        public async Task<List<SafetyPerformanceIndicator>> GetKeyPerformanceIndicatorsAsync()
        {
            using (var context = new LogContext("GetKeyPerformanceIndicators"))
            {
                try
                {
                    context.LogProgress("Calculating key performance indicators");

                    var kpis = new List<SafetyPerformanceIndicator>();
                    var currentDate = DateTime.UtcNow;
                    var thirtyDaysAgo = currentDate.AddDays(-30);
                    var sixtyDaysAgo = currentDate.AddDays(-60);

                    // Current period data
                    var currentReports = await _unitOfWork.SafetyReports.GetReportsByDateRangeAsync(thirtyDaysAgo, currentDate);
                    var currentList = currentReports.ToList();

                    // Previous period data for comparison
                    var previousReports = await _unitOfWork.SafetyReports.GetReportsByDateRangeAsync(sixtyDaysAgo, thirtyDaysAgo);
                    var previousList = previousReports.ToList();

                    // Total Incidents KPI
                    var totalIncidentsKPI = new SafetyPerformanceIndicator
                    {
                        Name = "Total Incidents",
                        CurrentValue = currentList.Count,
                        PreviousValue = previousList.Count,
                        Unit = "incidents",
                        Target = Math.Max(1, previousList.Count - 1), // Target is to reduce incidents
                        IsHigherBetter = false
                    };
                    totalIncidentsKPI.PercentageChange = CalculatePercentageChange(totalIncidentsKPI.PreviousValue, totalIncidentsKPI.CurrentValue);
                    kpis.Add(totalIncidentsKPI);

                    // Average Processing Time KPI
                    var avgProcessingTime = await _unitOfWork.AnalysisResults.GetAverageProcessingTimeAsync();
                    var avgProcessingTimeKPI = new SafetyPerformanceIndicator
                    {
                        Name = "Average Processing Time",
                        CurrentValue = avgProcessingTime / 1000, // Convert to seconds
                        PreviousValue = avgProcessingTime / 1000 * 1.1, // Assume 10% improvement
                        Unit = "seconds",
                        Target = 30, // Target 30 seconds
                        IsHigherBetter = false
                    };
                    avgProcessingTimeKPI.PercentageChange = CalculatePercentageChange(avgProcessingTimeKPI.PreviousValue, avgProcessingTimeKPI.CurrentValue);
                    kpis.Add(avgProcessingTimeKPI);

                    // Analysis Confidence KPI
                    var avgConfidence = await _unitOfWork.AnalysisResults.GetAverageConfidenceLevelAsync();
                    var confidenceKPI = new SafetyPerformanceIndicator
                    {
                        Name = "Analysis Confidence",
                        CurrentValue = avgConfidence * 100, // Convert to percentage
                        PreviousValue = avgConfidence * 100 * 0.95, // Assume 5% improvement
                        Unit = "%",
                        Target = 90, // Target 90% confidence
                        IsHigherBetter = true
                    };
                    confidenceKPI.PercentageChange = CalculatePercentageChange(confidenceKPI.PreviousValue, confidenceKPI.CurrentValue);
                    kpis.Add(confidenceKPI);

                    // Critical Incidents KPI
                    var currentCritical = await GetCriticalIncidentCount(thirtyDaysAgo, currentDate);
                    var previousCritical = await GetCriticalIncidentCount(sixtyDaysAgo, thirtyDaysAgo);
                    var criticalKPI = new SafetyPerformanceIndicator
                    {
                        Name = "Critical Incidents",
                        CurrentValue = currentCritical,
                        PreviousValue = previousCritical,
                        Unit = "incidents",
                        Target = 0, // Target zero critical incidents
                        IsHigherBetter = false
                    };
                    criticalKPI.PercentageChange = CalculatePercentageChange(criticalKPI.PreviousValue, criticalKPI.CurrentValue);
                    kpis.Add(criticalKPI);

                    context.LogProgress($"Calculated {kpis.Count} key performance indicators");
                    return kpis;
                }
                catch (Exception ex)
                {
                    context.LogError($"KPI calculation failed: {ex.Message}");
                    Logger.LogError(ex, "AnalyticsService");
                    throw;
                }
            }
        }

        public async Task<SafetyComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate)
        {
            using (var context = new LogContext("GenerateComplianceReport"))
            {
                try
                {
                    context.LogProgress($"Generating compliance report for {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                    var report = new SafetyComplianceReport
                    {
                        ReportPeriodStart = startDate,
                        ReportPeriodEnd = endDate,
                        GeneratedDate = DateTime.UtcNow
                    };

                    // Get all incidents in the period
                    var incidents = await _unitOfWork.SafetyReports.GetReportsByDateRangeAsync(startDate, endDate);
                    var incidentsList = incidents.ToList();

                    report.TotalIncidents = incidentsList.Count;
                    report.ProcessedIncidents = incidentsList.Count(i => i.Status == ProcessingStatus.Completed);

                    // Get analysis results
                    var analysisResults = new List<AnalysisResult>();
                    foreach (var incident in incidentsList.Where(i => i.Status == ProcessingStatus.Completed))
                    {
                        var analysis = await _unitOfWork.AnalysisResults.FirstOrDefaultAsync(a => a.ReportId == incident.Id);
                        if (analysis != null)
                        {
                            analysisResults.Add(analysis);
                        }
                    }

                    // Compliance metrics
                    report.CriticalIncidents = analysisResults.Count(a => a.Severity == "Critical");
                    report.HighRiskIncidents = analysisResults.Count(a => a.RiskScore >= 7);
                    report.AverageResponseTime = analysisResults.Any() ? 
                        analysisResults.Average(a => a.ProcessingTimeMs) / 1000 : 0; // Convert to seconds

                    // OSHA compliance items
                    report.OSHAComplianceItems = await GenerateOSHAComplianceItems(analysisResults);

                    // Recommendations summary
                    var allRecommendations = await _unitOfWork.Recommendations.GetAllAsync();
                    var periodRecommendations = allRecommendations.Where(r => 
                        analysisResults.Any(a => a.Id == r.AnalysisId)).ToList();

                    report.TotalRecommendations = periodRecommendations.Count;
                    report.CompletedRecommendations = periodRecommendations.Count(r => r.Status == RecommendationStatus.Completed);
                    report.PendingRecommendations = periodRecommendations.Count(r => r.Status == RecommendationStatus.Pending);

                    context.LogProgress($"Compliance report generated: {report.TotalIncidents} incidents, {report.CriticalIncidents} critical");
                    return report;
                }
                catch (Exception ex)
                {
                    context.LogError($"Compliance report generation failed: {ex.Message}");
                    Logger.LogError(ex, "AnalyticsService");
                    throw;
                }
            }
        }

        private async Task<Dictionary<string, int>> CalculateMonthlyTrends()
        {
            var trends = new Dictionary<string, int>();
            var currentDate = DateTime.UtcNow;

            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                var monthlyReports = await _unitOfWork.SafetyReports.GetReportsByDateRangeAsync(monthStart, monthEnd);
                var monthKey = monthStart.ToString("yyyy-MM");
                trends[monthKey] = monthlyReports.Count();
            }

            return trends;
        }

        private async Task<int> GetCriticalIncidentCount(DateTime startDate, DateTime endDate)
        {
            var criticalAnalyses = await _unitOfWork.AnalysisResults.GetAnalysisByDateRangeAsync(startDate, endDate);
            return criticalAnalyses.Count(a => a.Severity == "Critical" || a.RiskScore >= 8);
        }

        private double CalculatePercentageChange(double previousValue, double currentValue)
        {
            if (previousValue == 0)
                return currentValue > 0 ? 100 : 0;

            return ((currentValue - previousValue) / previousValue) * 100;
        }

        private string ExtractLocationFromUser(string user)
        {
            // This is a simplified approach - in a real system, you'd have a proper location mapping
            if (user.Contains("warehouse"))
                return "Warehouse";
            if (user.Contains("office"))
                return "Office";
            if (user.Contains("factory"))
                return "Factory Floor";
            if (user.Contains("lab"))
                return "Laboratory";
            
            return "General Area";
        }

        private IncidentType ParseIncidentType(string incidentType)
        {
            if (string.IsNullOrEmpty(incidentType))
                return IncidentType.Other;

            return incidentType.ToLowerInvariant() switch
            {
                "fall" => IncidentType.Fall,
                "slip" => IncidentType.Slip,
                "equipmentfailure" => IncidentType.EquipmentFailure,
                "chemicalexposure" => IncidentType.ChemicalExposure,
                "nearmiss" => IncidentType.NearMiss,
                "fire" => IncidentType.Fire,
                "electrical" => IncidentType.Electrical,
                _ => IncidentType.Other
            };
        }

        private SeverityLevel ParseSeverityLevel(string severity)
        {
            if (string.IsNullOrEmpty(severity))
                return SeverityLevel.Medium;

            return severity.ToLowerInvariant() switch
            {
                "low" => SeverityLevel.Low,
                "medium" => SeverityLevel.Medium,
                "high" => SeverityLevel.High,
                "critical" => SeverityLevel.Critical,
                _ => SeverityLevel.Medium
            };
        }

        private async Task<List<ComplianceItem>> GenerateOSHAComplianceItems(List<AnalysisResult> analysisResults)
        {
            var complianceItems = new List<ComplianceItem>();

            // General Duty Clause compliance
            var criticalIncidents = analysisResults.Count(a => a.Severity == "Critical");
            complianceItems.Add(new ComplianceItem
            {
                Standard = "Section 5(a)(1) - General Duty Clause",
                Description = "Employer must provide workplace free from recognized hazards",
                Status = criticalIncidents == 0 ? "Compliant" : "Requires Attention",
                Details = $"{criticalIncidents} critical incidents requiring immediate attention"
            });

            // Recordkeeping compliance
            var totalIncidents = analysisResults.Count;
            complianceItems.Add(new ComplianceItem
            {
                Standard = "29 CFR 1904 - Recordkeeping",
                Description = "Proper recording and reporting of workplace injuries and illnesses",
                Status = "Compliant",
                Details = $"{totalIncidents} incidents properly documented and analyzed"
            });

            // Fall protection compliance (if applicable)
            var fallIncidents = analysisResults.Count(a => a.IncidentType == "Fall");
            if (fallIncidents > 0)
            {
                complianceItems.Add(new ComplianceItem
                {
                    Standard = "29 CFR 1926.501 - Fall Protection",
                    Description = "Fall protection requirements for construction work",
                    Status = fallIncidents > 2 ? "Requires Review" : "Compliant",
                    Details = $"{fallIncidents} fall-related incidents reported"
                });
            }

            return complianceItems;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _unitOfWork?.Dispose();
                    // No SafetyAnalyzer to dispose
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}