using System.Collections.Generic;
using SafetyAI.Models.Enums;

namespace SafetyAI.Models.DTOs
{
    public class SafetyAnalysisResult
    {
        public IncidentType IncidentType { get; set; }
        public SeverityLevel Severity { get; set; }
        public int RiskScore { get; set; } // 1-10 scale
        public string Summary { get; set; }
        public List<string> KeyFactors { get; set; }
        public double AnalysisConfidence { get; set; }
        public List<SafetyRecommendation> Recommendations { get; set; }
        public ComplianceMapping ComplianceMapping { get; set; }

        public SafetyAnalysisResult()
        {
            KeyFactors = new List<string>();
            Recommendations = new List<SafetyRecommendation>();
        }
    }

    public class SafetyRecommendation
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public decimal? EstimatedCost { get; set; }
        public int? EstimatedTimeHours { get; set; }
        public string ResponsibleRole { get; set; }
    }

    public class ComplianceMapping
    {
        public List<string> OSHAStandards { get; set; }
        public List<string> ISO45001Requirements { get; set; }
        public List<string> LocalRegulations { get; set; }

        public ComplianceMapping()
        {
            OSHAStandards = new List<string>();
            ISO45001Requirements = new List<string>();
            LocalRegulations = new List<string>();
        }
    }
}