using System;
using System.Collections.Generic;
using System.Linq;

namespace SafetyAI.Models.DTOs
{
    public class SafetyComplianceReport
    {
        public DateTime ReportPeriodStart { get; set; }
        public DateTime ReportPeriodEnd { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int TotalIncidents { get; set; }
        public int ProcessedIncidents { get; set; }
        public int CriticalIncidents { get; set; }
        public int HighRiskIncidents { get; set; }
        public double AverageResponseTime { get; set; }
        public int TotalRecommendations { get; set; }
        public int CompletedRecommendations { get; set; }
        public int PendingRecommendations { get; set; }
        public List<ComplianceItem> OSHAComplianceItems { get; set; }
        public double ComplianceScore => CalculateComplianceScore();

        public SafetyComplianceReport()
        {
            OSHAComplianceItems = new List<ComplianceItem>();
        }

        private double CalculateComplianceScore()
        {
            if (OSHAComplianceItems.Count == 0)
                return 100;

            var compliantItems = OSHAComplianceItems.Where(item => item.Status == "Compliant").Count();
            return (double)compliantItems / OSHAComplianceItems.Count * 100;
        }
    }

    public class ComplianceItem
    {
        public string Standard { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } // Compliant, Requires Attention, Non-Compliant
        public string Details { get; set; }
    }
}