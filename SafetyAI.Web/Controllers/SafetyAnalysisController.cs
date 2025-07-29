using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using SafetyAI.Models.DTOs;
using SafetyAI.Services.Interfaces;
using SafetyAI.Web.App_Start;
using SafetyAI.Services.Infrastructure;

namespace SafetyAI.Web.Controllers
{
    [RoutePrefix("api/v1/safety")]
    public class SafetyAnalysisController : ApiController
    {
        private readonly IDocumentProcessor _documentProcessor;
        private readonly IAnalyticsService _analyticsService;

        public SafetyAnalysisController()
        {
            _documentProcessor = DependencyConfig.GetService<IDocumentProcessor>();
            _analyticsService = DependencyConfig.GetService<IAnalyticsService>();
        }

        /// <summary>
        /// Analyze a safety document
        /// </summary>
        /// <returns>Analysis results</returns>
        [HttpPost]
        [Route("analyze")]
        public async Task<IHttpActionResult> AnalyzeDocument()
        {
            try
            {
                // Check if request contains multipart/form-data
                if (!Request.Content.IsMimeMultipartContent())
                {
                    return BadRequest("Request must contain multipart/form-data");
                }

                // Read the multipart content
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                if (provider.Contents.Count == 0)
                {
                    return BadRequest("No file provided");
                }

                var fileContent = provider.Contents[0];
                var fileBytes = await fileContent.ReadAsByteArrayAsync();
                var fileName = fileContent.Headers.ContentDisposition?.FileName?.Trim('"') ?? "unknown";
                var contentType = fileContent.Headers.ContentType?.MediaType ?? "application/octet-stream";

                // Validate file
                if (fileBytes.Length == 0)
                {
                    return BadRequest("File is empty");
                }

                if (fileBytes.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    return BadRequest("File size exceeds 10MB limit");
                }

                // Process document
                using (var documentProcessor = DependencyConfig.GetService<IDocumentProcessor>())
                {
                    var documentResult = await documentProcessor.ProcessDocumentAsync(fileBytes, fileName);

                    if (!documentResult.IsSuccess)
                    {
                        return BadRequest($"Document processing failed: {documentResult.ErrorMessage}");
                    }

                    // Mock safety analysis for development
                    var metadata = new DocumentMetadata
                    {
                        FileName = fileName,
                        ContentType = contentType,
                        FileSize = fileBytes.Length,
                        UploadedBy = "API User",
                        UploadedDate = DateTime.UtcNow
                    };

                    // Create mock analysis result
                    var analysisResult = new SafetyAnalysisResult
                    {
                        IncidentType = SafetyAI.Models.Enums.IncidentType.Fall,
                        Severity = SafetyAI.Models.Enums.SeverityLevel.Medium, 
                        RiskScore = 6,
                        Summary = "Mock API analysis: Potential safety incident identified",
                        AnalysisConfidence = 0.85,
                        KeyFactors = new System.Collections.Generic.List<string> { "Environmental hazard", "Equipment issue", "Procedural gap" },
                        Recommendations = new System.Collections.Generic.List<SafetyRecommendation>
                        {
                            new SafetyRecommendation
                            {
                                Type = "Immediate",
                                Description = "Implement immediate safety measures",
                                Priority = "High",
                                EstimatedCost = 200,
                                EstimatedTimeHours = 4,
                                ResponsibleRole = "Safety Manager"
                            }
                        }
                    };

                        var response = new
                        {
                            success = true,
                            data = new
                            {
                                fileName = fileName,
                                extractedText = documentResult.ExtractedText,
                                analysis = new
                                {
                                    summary = analysisResult.Summary,
                                    incidentType = analysisResult.IncidentType?.ToString(),
                                    severity = analysisResult.Severity?.ToString(),
                                    riskScore = analysisResult.RiskScore,
                                    confidence = analysisResult.AnalysisConfidence,
                                    keyFactors = analysisResult.KeyFactors,
                                    recommendations = analysisResult.Recommendations
                                },
                                processedAt = DateTime.UtcNow
                            }
                        };

                        return Ok(response);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SafetyAnalysisAPI");
                return InternalServerError(new Exception("An error occurred while processing the document"));
            }
        }

        /// <summary>
        /// Get historical analytics data
        /// </summary>
        /// <param name="days">Number of days to analyze (default: 30)</param>
        /// <returns>Historical analytics</returns>
        [HttpGet]
        [Route("analytics/historical")]
        public async Task<IHttpActionResult> GetHistoricalAnalytics(int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                {
                    return BadRequest("Days parameter must be between 1 and 365");
                }

                using (var analyticsService = DependencyConfig.GetService<IAnalyticsService>())
                {
                    var endDate = DateTime.UtcNow;
                    var startDate = endDate.AddDays(-days);

                    var dashboardMetrics = await analyticsService.GetDashboardMetricsAsync(startDate, endDate);
                    var trendAnalysis = await analyticsService.AnalyzeHistoricalTrendsAsync(days);
                    var kpis = await analyticsService.GetKeyPerformanceIndicatorsAsync();

                    var response = new
                    {
                        success = true,
                        data = new
                        {
                            period = new
                            {
                                startDate = startDate,
                                endDate = endDate,
                                days = days
                            },
                            metrics = new
                            {
                                totalReports = dashboardMetrics.TotalReports,
                                criticalIncidents = dashboardMetrics.CriticalIncidents,
                                averageRiskScore = dashboardMetrics.AverageRiskScore,
                                incidentTypeDistribution = dashboardMetrics.IncidentTypeDistribution,
                                severityDistribution = dashboardMetrics.SeverityDistribution,
                                monthlyTrends = dashboardMetrics.MonthlyTrends
                            },
                            trends = new
                            {
                                totalIncidents = trendAnalysis.TotalIncidents,
                                averageRiskScore = trendAnalysis.AverageRiskScore,
                                riskScoreTrend = trendAnalysis.RiskScoreTrend,
                                incidentTypeTrends = trendAnalysis.IncidentTypeTrends,
                                identifiedPatterns = trendAnalysis.IdentifiedPatterns
                            },
                            kpis = kpis
                        },
                        generatedAt = DateTime.UtcNow
                    };

                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HistoricalAnalyticsAPI");
                return InternalServerError(new Exception("An error occurred while retrieving analytics data"));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _documentProcessor?.Dispose();
                _analyticsService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}