using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using SafetyAI.Data.Context;
using SafetyAI.Models.Entities;
using SafetyAI.Services.Infrastructure;

namespace SafetyAI.Web
{
    public partial class Results : Page
    {
        private Guid ReportId
        {
            get
            {
                if (Guid.TryParse(Request.QueryString["reportId"], out Guid id))
                    return id;
                return Guid.Empty;
            }
        }

        protected async void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                await LoadReportResults();
            }
        }

        private async Task LoadReportResults()
        {
            try
            {
                if (ReportId == Guid.Empty)
                {
                    ShowNotFound();
                    return;
                }

                using (var context = new SafetyAIDbContext())
                {
                    // Debug: Let's see what reports exist
                    var allReports = context.SafetyReports.Select(r => new { r.Id, r.FileName }).ToList();
                    System.Diagnostics.Debug.WriteLine($"All reports in database: {string.Join(", ", allReports.Select(r => $"{r.Id}:{r.FileName}"))}");
                    
                    // Debug: Let's see what analysis results exist
                    var allAnalysisResults = context.AnalysisResults.Select(ar => new { ar.Id, ar.ReportId, ar.Summary }).ToList();
                    System.Diagnostics.Debug.WriteLine($"All analysis results in database: {string.Join(", ", allAnalysisResults.Select(ar => $"{ar.Id}:{ar.ReportId}:{ar.Summary}"))}");
                    
                    // First, let's try to get the report without includes to see if it exists
                    var report = context.SafetyReports.FirstOrDefault(r => r.Id == ReportId);
                    
                    if (report == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Report with ID {ReportId} not found");
                        ShowNotFound();
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"Found report: {report.FileName}");

                    // Now let's manually load the analysis results
                    var analysisResults = context.AnalysisResults
                        .Where(ar => ar.ReportId == ReportId)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"Found {analysisResults.Count} analysis results for report {ReportId}");

                    // Load recommendations for each analysis result
                    foreach (var analysis in analysisResults)
                    {
                        analysis.Recommendations = context.Recommendations
                            .Where(r => r.AnalysisId == analysis.Id)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"Analysis {analysis.Id} has {analysis.Recommendations.Count} recommendations");
                    }

                    // Manually set the analysis results
                    report.AnalysisResults = analysisResults;

                    DisplayReportResults(report);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Results");
                ShowError("An error occurred while loading the analysis results. Please try again.");
            }
        }

        private void DisplayReportResults(SafetyReport report)
        {
            pnlResults.Visible = true;
            pnlNotFound.Visible = false;
            pnlError.Visible = false;

            // File Information
            lblFileName.Text = report.FileName;
            lblUploadedBy.Text = report.UploadedBy ?? "Unknown";
            lblUploadDate.Text = report.UploadedDate.ToString("yyyy-MM-dd HH:mm");

            // Status Information
            lblStatus.Text = report.Status.ToString();

            System.Diagnostics.Debug.WriteLine($"Report ID: {report.Id}");
            System.Diagnostics.Debug.WriteLine($"Analysis Results Count: {report.AnalysisResults?.Count ?? 0}");
            
            var latestAnalysis = report.AnalysisResults?.OrderByDescending(ar => ar.AnalysisDate).FirstOrDefault();
            
            if (latestAnalysis != null)
            {
                System.Diagnostics.Debug.WriteLine($"Latest Analysis ID: {latestAnalysis.Id}");
                System.Diagnostics.Debug.WriteLine($"Latest Analysis Summary: {latestAnalysis.Summary}");
                System.Diagnostics.Debug.WriteLine($"Latest Analysis Severity: {latestAnalysis.Severity}");
                
                lblAnalysisDate.Text = latestAnalysis.AnalysisDate.ToString("yyyy-MM-dd HH:mm");

                // Analysis Summary
                lblSeverity.Text = latestAnalysis.Severity ?? "N/A";
                lblRiskScore.Text = latestAnalysis.RiskScore.ToString();

                lblAnalysisSummary.Text = !string.IsNullOrEmpty(latestAnalysis.Summary) 
                    ? latestAnalysis.Summary 
                    : "Analysis summary not available.";

                // Recommendations
                if (latestAnalysis.Recommendations.Any())
                {
                    rptRecommendations.DataSource = latestAnalysis.Recommendations.OrderBy(r => GetPriorityOrder(r.Priority));
                    rptRecommendations.DataBind();
                    pnlNoRecommendations.Visible = false;
                }
                else
                {
                    pnlNoRecommendations.Visible = true;
                }
            }
            else
            {
                lblAnalysisDate.Text = "Not analyzed";
                lblSeverity.Text = "N/A";
                lblRiskScore.Text = "N/A";
                lblAnalysisSummary.Text = "This report has not been analyzed yet.";
                pnlNoRecommendations.Visible = true;
            }
        }

        private void ShowNotFound()
        {
            pnlNotFound.Visible = true;
            pnlResults.Visible = false;
            pnlError.Visible = false;
        }

        private void ShowError(string message)
        {
            pnlError.Visible = true;
            lblError.Text = message;
            pnlResults.Visible = false;
            pnlNotFound.Visible = false;
        }

        private int GetPriorityOrder(string priority)
        {
            switch (priority?.ToLower())
            {
                case "critical":
                    return 1;
                case "high":
                    return 2;
                case "medium":
                    return 3;
                case "low":
                    return 4;
                default:
                    return 5;
            }
        }

        protected void btnBackToHistory_Click(object sender, EventArgs e)
        {
            Response.Redirect("History.aspx");
        }
    }
}