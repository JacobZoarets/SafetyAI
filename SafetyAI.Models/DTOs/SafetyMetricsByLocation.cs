using System;

namespace SafetyAI.Models.DTOs
{
    public class SafetyMetricsByLocation
    {
        public string Location { get; set; }
        public int TotalIncidents { get; set; }
        public int CriticalIncidents { get; set; }
        public double AverageRiskScore { get; set; }
        public string MostCommonIncidentType { get; set; }
        public DateTime LastIncidentDate { get; set; }
        public double IncidentRate { get; set; } // Incidents per time period
        public int TrendDirection { get; set; } // -1 decreasing, 0 stable, 1 increasing
    }
}