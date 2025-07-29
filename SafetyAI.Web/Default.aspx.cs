using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using SafetyAI.Models.Enums;
using SafetyAI.Models.DTOs;
using SafetyAI.Data.Interfaces;
using SafetyAI.Web.App_Start;
using SafetyAI.Services.Interfaces;
using SafetyAI.Data.Context;

namespace SafetyAI.Web
{
    public partial class Default : Page
    {
        private IUnitOfWork _unitOfWork;

        protected async void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Always load dashboard data when returning to the page
                await LoadDashboardDataAsync();
            }
        }

        protected async void btnUpload_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("btnUpload_Click called!");
            
            // Prevent multiple submissions
            btnUpload.Enabled = false;
            btnUpload.Text = "Processing...";
            
            try
            {
                if (fileUpload.HasFile)
                {
                    ShowMessage($"Starting to process file: {fileUpload.FileName} ({fileUpload.PostedFile.ContentType})", "info");
                    
                    // Use direct database context to avoid disposal issues
                    using (var context = new SafetyAIDbContext())
                    {
                        // Check for duplicate filename to prevent reprocessing
                        var existingReport = context.SafetyReports.FirstOrDefault(r => r.FileName == fileUpload.FileName);
                        if (existingReport != null)
                        {
                            ShowMessage($"File '{fileUpload.FileName}' has already been processed.", "warning");
                            return;
                        }
                        
                        var documentProcessor = DependencyConfig.GetService<SafetyAI.Services.Interfaces.IDocumentProcessor>();
                        if (documentProcessor == null)
                        {
                            ShowMessage("Document processor service is not available.", "danger");
                            return;
                        }

                        // Process the uploaded file
                        ShowMessage("Processing document with AI...", "info");
                        var result = await documentProcessor.ProcessDocumentAsync(
                            fileUpload.FileContent, 
                            fileUpload.FileName, 
                            fileUpload.PostedFile.ContentType);

                        if (result.IsSuccess)
                        {
                            ShowMessage("Document processed successfully! Saving to database...", "info");
                            
                            // Save to database
                            var safetyReport = new SafetyAI.Models.Entities.SafetyReport
                            {
                                FileName = fileUpload.FileName,
                                FileSize = fileUpload.PostedFile.ContentLength,
                                FileType = fileUpload.PostedFile.ContentType,
                                ExtractedText = result.ExtractedText,
                                Status = SafetyAI.Models.Enums.ProcessingStatus.Processing, // Set to processing initially
                                UploadedBy = Session["UserId"]?.ToString() ?? "Anonymous",
                                ProcessedDate = DateTime.UtcNow
                            };

                            context.SafetyReports.Add(safetyReport);
                            await context.SaveChangesAsync();

                            // Now perform safety analysis if we have extracted text
                            if (!string.IsNullOrWhiteSpace(result.ExtractedText))
                            {
                                ShowMessage("Performing safety analysis...", "info");
                                
                                try
                                {
                                    // Mock safety analysis result for development purposes
                                    var analysisResult = new SafetyAI.Models.DTOs.SafetyAnalysisResult
                                    {
                                        IncidentType = SafetyAI.Models.Enums.IncidentType.Fall,
                                        Severity = SafetyAI.Models.Enums.SeverityLevel.Medium,
                                        RiskScore = 6,
                                        Summary = "Mock analysis: Potential slip and fall incident identified in the workplace area.",
                                        AnalysisConfidence = 0.85,
                                        KeyFactors = new List<string> { "Wet floor", "Inadequate signage", "Poor lighting" },
                                        ComplianceMapping = new SafetyAI.Models.DTOs.ComplianceMapping
                                        {
                                            OSHAStandards = new List<string> { "29 CFR 1910.22" },
                                            ISO45001Requirements = new List<string> { "Section 8.1.2" },
                                            LocalRegulations = new List<string> { "Local safety code 101" }
                                        }
                                    };

                                    // Add mock recommendations
                                    var mockRecommendations = new List<SafetyAI.Models.DTOs.SafetyRecommendation>
                                    {
                                        new SafetyAI.Models.DTOs.SafetyRecommendation
                                        {
                                            Type = "Immediate",
                                            Description = "Install warning signs in wet areas",
                                            Priority = "High",
                                            EstimatedCost = 150,
                                            EstimatedTimeHours = 2,
                                            ResponsibleRole = "Facilities Manager"
                                        },
                                        new SafetyAI.Models.DTOs.SafetyRecommendation
                                        {
                                            Type = "Administrative",
                                            Description = "Implement regular floor inspection schedule",
                                            Priority = "Medium",
                                            EstimatedCost = 500,
                                            EstimatedTimeHours = 8,
                                            ResponsibleRole = "Safety Coordinator"
                                        }
                                    };

                                    analysisResult.Recommendations = mockRecommendations;

                                    // Create mock Gemini API response structure for display
                                    var mockGeminiResponse = new
                                    {
                                        Model = "gemini-2.5-flash-latest",
                                        ProcessingTimeMs = (int)result.ProcessingTime.TotalMilliseconds,
                                        RequestTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                        Response = new
                                        {
                                            Candidates = new[]
                                            {
                                                new
                                                {
                                                    Content = new
                                                    {
                                                        Parts = new[]
                                                        {
                                                            new
                                                            {
                                                                Text = Newtonsoft.Json.JsonConvert.SerializeObject(analysisResult, Newtonsoft.Json.Formatting.Indented)
                                                            }
                                                        },
                                                        Role = "model"
                                                    },
                                                    FinishReason = "STOP",
                                                    Index = 0,
                                                    SafetyRatings = new[]
                                                    {
                                                        new { Category = "HARM_CATEGORY_HARASSMENT", Probability = "NEGLIGIBLE" },
                                                        new { Category = "HARM_CATEGORY_HATE_SPEECH", Probability = "NEGLIGIBLE" },
                                                        new { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Probability = "NEGLIGIBLE" },
                                                        new { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Probability = "NEGLIGIBLE" }
                                                    }
                                                }
                                            },
                                            PromptFeedback = new
                                            {
                                                SafetyRatings = new[]
                                                {
                                                    new { Category = "HARM_CATEGORY_HARASSMENT", Probability = "NEGLIGIBLE" },
                                                    new { Category = "HARM_CATEGORY_HATE_SPEECH", Probability = "NEGLIGIBLE" },
                                                    new { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Probability = "NEGLIGIBLE" },
                                                    new { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Probability = "NEGLIGIBLE" }
                                                }
                                            }
                                        },
                                        ExtractedText = result.ExtractedText ?? "",
                                        AnalysisMetadata = new
                                        {
                                            FileName = safetyReport.FileName,
                                            FileSize = safetyReport.FileSize,
                                            ProcessedAt = DateTime.UtcNow,
                                            ConfidenceScore = analysisResult.AnalysisConfidence,
                                            ProcessingVersion = "SafetyAI v1.0.0"
                                        }
                                    };

                                    // Serialize and display the mock response
                                    string jsonResponse = Newtonsoft.Json.JsonConvert.SerializeObject(mockGeminiResponse, Newtonsoft.Json.Formatting.Indented);
                                    litGeminiResponse.Text = System.Web.HttpUtility.HtmlEncode(jsonResponse);
                                    pnlGeminiResponse.Visible = true;

                                        // Save analysis result to database
                                        var dbAnalysisResult = new SafetyAI.Models.Entities.AnalysisResult
                                        {
                                            ReportId = safetyReport.Id,
                                            IncidentType = analysisResult.IncidentType.ToString(),
                                            Severity = analysisResult.Severity.ToString(),
                                            RiskScore = analysisResult.RiskScore,
                                            Summary = analysisResult.Summary,
                                            ConfidenceLevel = (decimal)analysisResult.AnalysisConfidence,
                                            ProcessingTimeMs = (int)result.ProcessingTime.TotalMilliseconds,
                                            AIModel = "Mock-Development"
                                        };

                                        context.AnalysisResults.Add(dbAnalysisResult);
                                        await context.SaveChangesAsync();

                                        // Save mock recommendations
                                        var recommendations = mockRecommendations;
                                        if (recommendations != null && recommendations.Count() > 0)
                                        {
                                            foreach (var rec in recommendations)
                                            {
                                                var dbRecommendation = new SafetyAI.Models.Entities.Recommendation
                                                {
                                                    AnalysisId = dbAnalysisResult.Id,
                                                    RecommendationType = rec.Type,
                                                    Description = rec.Description,
                                                    Priority = rec.Priority,
                                                    EstimatedCost = rec.EstimatedCost,
                                                    EstimatedTimeHours = rec.EstimatedTimeHours,
                                                    ResponsibleRole = rec.ResponsibleRole,
                                                    Status = SafetyAI.Models.Enums.RecommendationStatus.Pending
                                                };
                                                context.Recommendations.Add(dbRecommendation);
                                            }
                                            await context.SaveChangesAsync();
                                        }

                                        // Update report status to completed
                                        safetyReport.Status = result.RequiresHumanReview ? 
                                            SafetyAI.Models.Enums.ProcessingStatus.RequiresReview : 
                                            SafetyAI.Models.Enums.ProcessingStatus.Completed;
                                        await context.SaveChangesAsync();

                                        var message = $"Analysis completed! Incident Type: {analysisResult.IncidentType}, Severity: {analysisResult.Severity}, Risk Score: {analysisResult.RiskScore}/10";
                                        
                                        if (recommendations != null && recommendations.Count() > 0)
                                        {
                                            message += $"<br/>Generated {recommendations.Count()} safety recommendations.";
                                        }
                                        
                                        ShowMessage(message, "success");
                                }
                                catch (Exception analysisEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Analysis error: {analysisEx}");
                                    safetyReport.Status = SafetyAI.Models.Enums.ProcessingStatus.Failed;
                                    await context.SaveChangesAsync();
                                    ShowMessage($"Safety analysis failed: {analysisEx.Message}", "warning");
                                }
                            }
                            else
                            {
                                safetyReport.Status = SafetyAI.Models.Enums.ProcessingStatus.Failed;
                                await context.SaveChangesAsync();
                                ShowMessage("No text was extracted from the document for analysis.", "warning");
                            }
                            
                            // Refresh dashboard data and reset session flag
                            Session["DashboardLoaded"] = null;
                        }
                        else
                        {
                            ShowMessage($"File processing failed: {result.ErrorMessage}", "danger");
                        }
                    }
                }
                else
                {
                    ShowMessage("Please select a file to upload.", "warning");
                }
            }
            catch (Exception ex)
            {
                SafetyAI.Services.Infrastructure.Logger.LogError(ex, "FileUpload");
                ShowMessage($"Error processing file: {ex.GetType().Name}: {ex.Message}", "danger");
                System.Diagnostics.Debug.WriteLine($"Upload error: {ex}");
            }
            finally
            {
                // Re-enable the button
                btnUpload.Enabled = true;
                btnUpload.Text = "Analyze Report";
            }
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            // Clear the file upload control
            Page.Response.Redirect(Page.Request.Url.ToString(), true);
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // Use direct database context to avoid disposal issues
                using (var context = new SafetyAIDbContext())
                {
                    // Load statistics from database
                    var allReports = context.SafetyReports.ToList();
                    var totalReports = allReports.Count();

                    var pendingReports = context.SafetyReports.Count(r => r.Status == ProcessingStatus.Pending);
                    
                    var criticalAnalyses = context.AnalysisResults.Where(a => a.RiskScore >= 7).ToList();
                    var criticalCount = criticalAnalyses.Count();

                    // Update UI
                    lblTotalReports.Text = totalReports.ToString();
                    lblPendingReports.Text = pendingReports.ToString();
                    lblCriticalIncidents.Text = criticalCount.ToString();

                    // Load recent activity (last 5 reports)
                    var recentReports = context.SafetyReports
                        .OrderByDescending(r => r.ProcessedDate)
                        .Take(5)
                        .ToList();
                    rptRecentActivity.DataSource = recentReports;
                    rptRecentActivity.DataBind();
                }
                
                // Load reports data for the history section
                await LoadReportsData();
            }
            catch (Exception ex)
            {
                // Log detailed error information
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                SafetyAI.Services.Infrastructure.Logger.LogError(ex, "Dashboard");
                
                ShowMessage($"Database connection error: {ex.Message}. Please check SQL Server is running.", "danger");

                // Set default values
                lblTotalReports.Text = "0";
                lblPendingReports.Text = "0";  
                lblCriticalIncidents.Text = "0";
                
                // Clear recent activity
                rptRecentActivity.DataSource = null;
                rptRecentActivity.DataBind();
            }
        }

        protected string GetStatusBadgeClass(object status)
        {
            if (status == null) return "secondary";

            var statusEnum = (ProcessingStatus)status;
            switch (statusEnum)
            {
                case ProcessingStatus.Completed:
                    return "success";
                case ProcessingStatus.Processing:
                    return "primary";
                case ProcessingStatus.Failed:
                    return "danger";
                case ProcessingStatus.RequiresReview:
                    return "warning";
                default:
                    return "secondary";
            }
        }

        private void ShowMessage(string message, string type)
        {
            // Map "info" to Bootstrap's "info" class
            var bootstrapType = type == "info" ? "info" : type;
            
            lblMessage.Text = message;
            lblMessage.CssClass = $"alert alert-{bootstrapType}";
            pnlMessage.Visible = true;
            
            // Also log debug messages
            System.Diagnostics.Debug.WriteLine($"[{type.ToUpper()}] {message}");
        }

        private MockAnalysisResult CreateMockAnalysisResult(string extractedText)
        {
            var random = new Random();
            var lowerText = extractedText?.ToLowerInvariant() ?? "";
            
            // Simple keyword-based mock analysis
            string incidentType = "Other";
            string severity = "Medium";
            int riskScore = 5;
            
            if (lowerText.Contains("fall") || lowerText.Contains("fell"))
            {
                incidentType = "Fall";
                severity = "High";
                riskScore = 7;
            }
            else if (lowerText.Contains("slip") || lowerText.Contains("wet"))
            {
                incidentType = "Slip";
                severity = "Medium";
                riskScore = 5;
            }
            else if (lowerText.Contains("equipment") || lowerText.Contains("machine"))
            {
                incidentType = "EquipmentFailure";
                severity = "High";
                riskScore = 6;
            }
            else if (lowerText.Contains("fire") || lowerText.Contains("burn"))
            {
                incidentType = "Fire";
                severity = "Critical";
                riskScore = 9;
            }
            else if (lowerText.Contains("chemical"))
            {
                incidentType = "ChemicalExposure";
                severity = "Critical";
                riskScore = 8;
            }
            
            return new MockAnalysisResult
            {
                IncidentType = incidentType,
                Severity = severity,
                RiskScore = riskScore,
                Summary = $"Mock analysis identified a {incidentType} incident with {severity} severity. Risk assessment score: {riskScore}/10.",
                ConfidenceLevel = 0.85m
            };
        }
        
        private List<MockRecommendation> CreateMockRecommendations(MockAnalysisResult analysis)
        {
            var recommendations = new List<MockRecommendation>();
            
            // Always add investigation recommendation
            recommendations.Add(new MockRecommendation
            {
                Type = "Administrative",
                Description = "Conduct thorough incident investigation and document findings",
                Priority = "High",
                EstimatedCost = 200,
                EstimatedTimeHours = 4,
                ResponsibleRole = "Safety Manager"
            });
            
            // Add specific recommendations based on incident type
            switch (analysis.IncidentType)
            {
                case "Fall":
                    recommendations.Add(new MockRecommendation
                    {
                        Type = "Preventive",
                        Description = "Install fall protection systems and ensure proper use of safety harnesses",
                        Priority = "Critical",
                        EstimatedCost = 2500,
                        EstimatedTimeHours = 16,
                        ResponsibleRole = "Safety Manager"
                    });
                    break;
                case "Slip":
                    recommendations.Add(new MockRecommendation
                    {
                        Type = "Preventive",
                        Description = "Improve floor surfaces and implement spill cleanup procedures",
                        Priority = "High",
                        EstimatedCost = 1200,
                        EstimatedTimeHours = 8,
                        ResponsibleRole = "Facility Manager"
                    });
                    break;
                case "EquipmentFailure":
                    recommendations.Add(new MockRecommendation
                    {
                        Type = "Corrective",
                        Description = "Conduct equipment inspection and implement preventive maintenance",
                        Priority = "Critical",
                        EstimatedCost = 3000,
                        EstimatedTimeHours = 24,
                        ResponsibleRole = "Maintenance Supervisor"
                    });
                    break;
                case "Fire":
                    recommendations.Add(new MockRecommendation
                    {
                        Type = "Emergency",
                        Description = "Review fire safety procedures and conduct emergency drills",
                        Priority = "Critical",
                        EstimatedCost = 1500,
                        EstimatedTimeHours = 12,
                        ResponsibleRole = "Fire Safety Officer"
                    });
                    break;
                case "ChemicalExposure":
                    recommendations.Add(new MockRecommendation
                    {
                        Type = "Safety",
                        Description = "Review chemical handling procedures and PPE requirements",
                        Priority = "Critical",
                        EstimatedCost = 800,
                        EstimatedTimeHours = 16,
                        ResponsibleRole = "EHS Manager"
                    });
                    break;
                default:
                    recommendations.Add(new MockRecommendation
                    {
                        Type = "Training",
                        Description = "Provide general safety awareness training to all staff",
                        Priority = "Medium",
                        EstimatedCost = 500,
                        EstimatedTimeHours = 8,
                        ResponsibleRole = "Training Coordinator"
                    });
                    break;
            }
            
            return recommendations;
        }

        protected override void OnUnload(EventArgs e)
        {
            // No need to dispose anything since we use fresh contexts
            base.OnUnload(e);
        }

        // History functionality methods
        protected async void ddlStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadReportsData();
        }

        protected async void btnSearch_Click(object sender, EventArgs e)
        {
            await LoadReportsData();
        }

        protected async void gvReports_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ViewReport")
            {
                int reportId = Convert.ToInt32(e.CommandArgument);
                Response.Redirect($"Results.aspx?id={reportId}");
            }
            else if (e.CommandName == "ReanalyzeReport")
            {
                int reportId = Convert.ToInt32(e.CommandArgument);
                // TODO: Implement re-analysis functionality
                ShowMessage("Re-analysis functionality not yet implemented.", "info");
            }
        }

        private async Task LoadReportsData()
        {
            try
            {
                using (var context = new SafetyAIDbContext())
                {
                    var reports = context.SafetyReports.Include("AnalysisResults").ToList();

                    // Apply filters if any
                    var statusFilter = ddlStatusFilter.SelectedValue;
                    var searchTerm = txtSearch.Text.Trim();

                    if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<ProcessingStatus>(statusFilter, out var status))
                    {
                        reports = reports.Where(r => r.Status == status).ToList();
                    }

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        reports = reports.Where(r =>
                            r.FileName.Contains(searchTerm) ||
                            r.UploadedBy.Contains(searchTerm)).ToList();
                    }

                    var orderedReports = reports.OrderByDescending(r => r.UploadedDate).Take(10).ToList(); // Limit to 10 for dashboard
                    gvReports.DataSource = orderedReports;
                    gvReports.DataBind();
                }
            }
            catch (Exception ex)
            {
                SafetyAI.Services.Infrastructure.Logger.LogError(ex, "Dashboard");
                System.Diagnostics.Debug.WriteLine($"Dashboard LoadReportsData Error: {ex.Message}");
            }
        }

        protected string GetLatestSeverity(object analysisResults)
        {
            if (analysisResults == null) return "N/A";
            
            try
            {
                var results = analysisResults as System.Collections.IEnumerable;
                if (results == null) return "N/A";
                
                // Check if there are any results
                var enumerator = results.GetEnumerator();
                if (!enumerator.MoveNext()) return "N/A";
                
                // Mock implementation - in real scenario, would get the latest severity
                return "Medium";
            }
            catch
            {
                return "N/A";
            }
        }

        protected object GetLatestRiskScore(object analysisResults)
        {
            if (analysisResults == null) return null;
            
            try
            {
                var results = analysisResults as System.Collections.IEnumerable;
                if (results == null) return null;
                
                // Check if there are any results
                var enumerator = results.GetEnumerator();
                if (!enumerator.MoveNext()) return null;
                
                // Mock implementation - in real scenario, would get the latest risk score
                return 6;
            }
            catch
            {
                return null;
            }
        }

        protected string GetSeverityBadgeClass(string severity)
        {
            switch (severity?.ToLower())
            {
                case "critical": return "danger";
                case "high": return "warning";
                case "medium": return "info";
                case "low": return "success";
                default: return "secondary";
            }
        }
    }
    
    // Mock data classes
    public class MockAnalysisResult
    {
        public string IncidentType { get; set; }
        public string Severity { get; set; }
        public int RiskScore { get; set; }
        public string Summary { get; set; }
        public decimal ConfidenceLevel { get; set; }
    }
    
    public class MockRecommendation
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public decimal? EstimatedCost { get; set; }
        public int? EstimatedTimeHours { get; set; }
        public string ResponsibleRole { get; set; }
    }
}