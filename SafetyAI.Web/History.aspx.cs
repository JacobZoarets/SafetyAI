using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using SafetyAI.Data.Interfaces;
using SafetyAI.Models.Enums;
using SafetyAI.Web.App_Start;
using SafetyAI.Data.Context;

namespace SafetyAI.Web
{
    public partial class History : Page
    {
        protected async void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                await LoadReportsData();
                await LoadQuickStats();
                await LoadRecentActivity();
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

                    var orderedReports = reports.OrderByDescending(r => r.UploadedDate).ToList();
                    gvReports.DataSource = orderedReports;
                    gvReports.DataBind();
                }
            }
            catch (Exception ex)
            {
                SafetyAI.Services.Infrastructure.Logger.LogError(ex, "History");
                // Show error message to user
                System.Diagnostics.Debug.WriteLine($"History LoadReportsData Error: {ex.Message}");
            }
        }

        private async Task LoadQuickStats()
        {
            try
            {
                using (var context = new SafetyAIDbContext())
                {
                    var reports = context.SafetyReports.Include("AnalysisResults").ToList();

                    lblTotalReports.Text = reports.Count().ToString();
                    lblPendingReports.Text = reports.Count(r => r.Status == ProcessingStatus.Pending || r.Status == ProcessingStatus.Processing).ToString();
                    lblCriticalIncidents.Text = reports.Count(r => r.AnalysisResults.Any(ar => ar.Severity == "Critical")).ToString();
                    lblCompletedToday.Text = reports.Count(r => r.UploadedDate.Date == DateTime.Today && r.Status == ProcessingStatus.Completed).ToString();
                }
            }
            catch (Exception ex)
            {
                SafetyAI.Services.Infrastructure.Logger.LogError(ex, "QuickStats");
                System.Diagnostics.Debug.WriteLine($"History LoadQuickStats Error: {ex.Message}");

                // Set default values on error
                lblTotalReports.Text = "0";
                lblPendingReports.Text = "0";
                lblCriticalIncidents.Text = "0";
                lblCompletedToday.Text = "0";
            }
        }

        private async Task LoadRecentActivity()
        {
            try
            {
                using (var context = new SafetyAIDbContext())
                {
                    var recentReports = context.SafetyReports
                        .OrderByDescending(r => r.UploadedDate)
                        .Take(5)
                        .ToList();
                    rptRecentActivity.DataSource = recentReports;
                    rptRecentActivity.DataBind();
                }
            }
            catch (Exception ex)
            {
                SafetyAI.Services.Infrastructure.Logger.LogError(ex, "RecentActivity");
                System.Diagnostics.Debug.WriteLine($"History LoadRecentActivity Error: {ex.Message}");
            }
        }

        protected async void ddlStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadReportsData();
        }

        protected async void btnSearch_Click(object sender, EventArgs e)
        {
            await LoadReportsData();
        }

        protected void gvReports_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ViewReport")
            {
                var reportId = e.CommandArgument.ToString();
                Response.Redirect($"Results.aspx?reportId={reportId}");
            }
            else if (e.CommandName == "ReanalyzeReport")
            {
                var reportId = e.CommandArgument.ToString();
                // TODO: Implement re-analysis functionality
                Response.Redirect($"Default.aspx?reanalyze={reportId}");
            }
        }



        protected string GetStatusBadgeClass(object status)
        {
            switch (status?.ToString()?.ToLower())
            {
                case "completed":
                    return "success";
                case "processing":
                    return "warning";
                case "pending":
                    return "info";
                case "failed":
                    return "danger";
                case "requiresreview":
                    return "warning";
                default:
                    return "secondary";
            }
        }

        protected string GetSeverityBadgeClass(object severity)
        {
            switch (severity?.ToString()?.ToLower())
            {
                case "critical":
                    return "danger";
                case "high":
                    return "warning";
                case "medium":
                    return "info";
                case "low":
                    return "success";
                default:
                    return "secondary";
            }
        }

        protected string GetActivityBadgeClass(object status)
        {
            return GetStatusBadgeClass(status);
        }

        protected string GetActivityIcon(object status)
        {
            switch (status?.ToString()?.ToLower())
            {
                case "completed":
                    return "check";
                case "processing":
                    return "spinner";
                case "pending":
                    return "clock";
                case "failed":
                    return "times";
                case "requiresreview":
                    return "exclamation";
                default:
                    return "question";
            }
        }

        protected string GetLatestSeverity(object analysisResults)
        {
            if (analysisResults is System.Collections.Generic.ICollection<SafetyAI.Models.Entities.AnalysisResult> results && results.Any())
            {
                var latest = results.OrderByDescending(ar => ar.AnalysisDate).FirstOrDefault();
                return latest?.Severity ?? "N/A";
            }
            return "N/A";
        }

        protected object GetLatestRiskScore(object analysisResults)
        {
            if (analysisResults is System.Collections.Generic.ICollection<SafetyAI.Models.Entities.AnalysisResult> results && results.Any())
            {
                var latest = results.OrderByDescending(ar => ar.AnalysisDate).FirstOrDefault();
                return latest?.RiskScore;
            }
            return null;
        }

        protected void btnExportCSV_Click(object sender, EventArgs e)
        {
            try
            {
                using (var context = new SafetyAIDbContext())
                {
                    var reports = context.SafetyReports.Include("AnalysisResults").ToList();

                    Response.ContentType = "text/csv";
                    Response.AddHeader("Content-Disposition", "attachment; filename=safety-reports.csv");

                    // Generate CSV content
                    var csv = "FileName,Status,UploadedBy,UploadedDate,Severity,RiskScore\n";
                    foreach (var report in reports)
                    {
                        var latestAnalysis = report.AnalysisResults.OrderByDescending(ar => ar.AnalysisDate).FirstOrDefault();
                        csv += $"{report.FileName},{report.Status},{report.UploadedBy},{report.UploadedDate:yyyy-MM-dd HH:mm},{latestAnalysis?.Severity ?? "N/A"},{latestAnalysis?.RiskScore ?? 0}\n";
                    }

                    Response.Write(csv);
                    Response.End();
                }
            }
            catch (Exception ex)
            {
                SafetyAI.Services.Infrastructure.Logger.LogError(ex, "ExportCSV");
            }
        }

        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                // For now, export as CSV with Excel MIME type
                Response.ContentType = "application/vnd.ms-excel";
                Response.AddHeader("Content-Disposition", "attachment; filename=safety-reports.xls");
                Response.Write("Excel export functionality will be implemented in future tasks");
                Response.End();
            }
            catch (Exception ex)
            {
                SafetyAI.Services.Infrastructure.Logger.LogError(ex, "ExportExcel");
            }
        }

        protected void btnExportPDF_Click(object sender, EventArgs e)
        {
            try
            {
                Response.ContentType = "application/pdf";
                Response.AddHeader("Content-Disposition", "attachment; filename=safety-reports.pdf");
                Response.Write("PDF export functionality will be implemented in future tasks");
                Response.End();
            }
            catch (Exception ex)
            {
                SafetyAI.Services.Infrastructure.Logger.LogError(ex, "ExportPDF");
            }
        }
    }
}