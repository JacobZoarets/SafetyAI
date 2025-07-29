using System;
using System.Collections.Generic;

namespace SafetyAI.Models.DTOs
{
    public class SafetyDashboardMetrics
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int CompletedReports { get; set; }
        public int FailedReports { get; set; }
        public int CriticalIncidents { get; set; }
        public double AverageRiskScore { get; set; }
        public Dictionary<string, int> IncidentTypeDistribution { get; set; }
        public Dictionary<string, int> SeverityDistribution { get; set; }
        public Dictionary<string, int> MonthlyTrends { get; set; }
        public List<SafetyRecommendation> TopRecommendations { get; set; }

        public SafetyDashboardMetrics()
        {
            IncidentTypeDistribution = new Dictionary<string, int>();
            SeverityDistribution = new Dictionary<string, int>();
            MonthlyTrends = new Dictionary<string, int>();
            TopRecommendations = new List<SafetyRecommendation>();
        }
    }
}