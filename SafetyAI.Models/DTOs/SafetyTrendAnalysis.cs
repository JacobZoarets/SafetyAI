using System;
using System.Collections.Generic;
using SafetyAI.Models.Enums;

namespace SafetyAI.Models.DTOs
{
    public class SafetyTrendAnalysis
    {
        public DateTime AnalysisDate { get; set; }
        public int TotalIncidents { get; set; }
        public int AnalysisPeriodDays { get; set; }
        public double AverageRiskScore { get; set; }
        public string RiskScoreTrend { get; set; }
        public Dictionary<IncidentType, int> IncidentTypeTrends { get; set; }
        public Dictionary<SeverityLevel, int> SeverityTrends { get; set; }
        public List<string> IdentifiedPatterns { get; set; }
        public List<SafetyRecommendation> TrendRecommendations { get; set; }
        public double ConfidenceLevel { get; set; }

        public SafetyTrendAnalysis()
        {
            AnalysisDate = DateTime.UtcNow;
            IncidentTypeTrends = new Dictionary<IncidentType, int>();
            SeverityTrends = new Dictionary<SeverityLevel, int>();
            IdentifiedPatterns = new List<string>();
            TrendRecommendations = new List<SafetyRecommendation>();
        }
    }
}