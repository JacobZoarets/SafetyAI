using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SafetyAI.Models.DTOs;
using SafetyAI.Models.Enums;
using SafetyAI.Services.Infrastructure;
using SafetyAI.Services.Interfaces;

namespace SafetyAI.Services.Implementation
{
    public class SafetyAnalyzer : ISafetyAnalyzer, IDisposable
    {
        private readonly IGeminiAPIClient _geminiClient;
        private bool _disposed = false;

        public SafetyAnalyzer(IGeminiAPIClient geminiClient)
        {
            _geminiClient = geminiClient ?? throw new ArgumentNullException(nameof(geminiClient));
        }

        public async Task<SafetyAnalysisResult> AnalyzeIncidentAsync(string extractedText, SafetyAI.Models.DTOs.DocumentMetadata metadata)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return CreateDefaultAnalysisResult("No text provided for analysis");
                }

                // Simple text analysis based on keywords
                var analysisResult = AnalyzeTextForSafetyIncidents(extractedText);

                // Add some basic recommendations
                analysisResult.Recommendations = GenerateBasicRecommendations(analysisResult);

                return analysisResult;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SafetyAnalyzer");
                return CreateDefaultAnalysisResult($"Analysis failed: {ex.Message}");
            }
        }

        public async Task<List<SafetyRecommendation>> GenerateRecommendationsAsync(SafetyAnalysisResult analysis)
        {
            try
            {
                var recommendations = new List<SafetyRecommendation>();

                // Generate recommendations based on incident type
                if (analysis.IncidentType == IncidentType.Fall)
                {
                    recommendations.Add(new SafetyRecommendation
                    {
                        Type = "Preventive",
                        Description = "Install fall protection systems and ensure proper use of safety harnesses",
                        Priority = "High",
                        EstimatedCost = 2500,
                        EstimatedTimeHours = 16,
                        ResponsibleRole = "Safety Manager"
                    });
                }
                else if (analysis.IncidentType == IncidentType.Slip)
                {
                    recommendations.Add(new SafetyRecommendation
                    {
                        Type = "Preventive",
                        Description = "Improve floor surfaces and implement spill cleanup procedures",
                        Priority = "Medium",
                        EstimatedCost = 1200,
                        EstimatedTimeHours = 8,
                        ResponsibleRole = "Facility Manager"
                    });
                }
                else if (analysis.IncidentType == IncidentType.EquipmentFailure)
                {
                    recommendations.Add(new SafetyRecommendation
                    {
                        Type = "Corrective",
                        Description = "Conduct equipment inspection and implement preventive maintenance",
                        Priority = "High",
                        EstimatedCost = 3000,
                        EstimatedTimeHours = 24,
                        ResponsibleRole = "Maintenance Supervisor"
                    });
                }
                else
                {
                    recommendations.Add(new SafetyRecommendation
                    {
                        Type = "Administrative",
                        Description = "Conduct incident investigation and implement safety training",
                        Priority = "Medium",
                        EstimatedCost = 500,
                        EstimatedTimeHours = 8,
                        ResponsibleRole = "Safety Manager"
                    });
                }

                return recommendations;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SafetyAnalyzer");
                return new List<SafetyRecommendation>();
            }
        }

        public async Task<ComplianceMapping> MapToStandardsAsync(SafetyAnalysisResult analysis)
        {
            return new ComplianceMapping
            {
                OSHAStandards = new List<string> { "29 CFR 1904 - Recording and Reporting" },
                ISO45001Requirements = new List<string> { "Clause 10.2 - Incident investigation" },
                LocalRegulations = new List<string> { "Local Safety Code - General Requirements" }
            };
        }

        private SafetyAnalysisResult AnalyzeTextForSafetyIncidents(string text)
        {
            var lowerText = text.ToLowerInvariant();
            var result = new SafetyAnalysisResult();

            // Determine incident type based on keywords
            if (lowerText.Contains("fall") || lowerText.Contains("fell") || lowerText.Contains("falling"))
            {
                result.IncidentType = IncidentType.Fall;
                result.Severity = SeverityLevel.High;
                result.RiskScore = 7;
            }
            else if (lowerText.Contains("slip") || lowerText.Contains("slipped") || lowerText.Contains("wet"))
            {
                result.IncidentType = IncidentType.Slip;
                result.Severity = SeverityLevel.Medium;
                result.RiskScore = 5;
            }
            else if (lowerText.Contains("equipment") || lowerText.Contains("machine") || lowerText.Contains("malfunction"))
            {
                result.IncidentType = IncidentType.EquipmentFailure;
                result.Severity = SeverityLevel.High;
                result.RiskScore = 6;
            }
            else if (lowerText.Contains("chemical") || lowerText.Contains("exposure") || lowerText.Contains("spill"))
            {
                result.IncidentType = IncidentType.ChemicalExposure;
                result.Severity = SeverityLevel.Critical;
                result.RiskScore = 8;
            }
            else if (lowerText.Contains("fire") || lowerText.Contains("burn") || lowerText.Contains("flame"))
            {
                result.IncidentType = IncidentType.Fire;
                result.Severity = SeverityLevel.Critical;
                result.RiskScore = 9;
            }
            else if (lowerText.Contains("electric") || lowerText.Contains("shock") || lowerText.Contains("voltage"))
            {
                result.IncidentType = IncidentType.Electrical;
                result.Severity = SeverityLevel.Critical;
                result.RiskScore = 8;
            }
            else if (lowerText.Contains("near miss") || lowerText.Contains("almost") || lowerText.Contains("close call"))
            {
                result.IncidentType = IncidentType.NearMiss;
                result.Severity = SeverityLevel.Low;
                result.RiskScore = 3;
            }
            else
            {
                result.IncidentType = IncidentType.Other;
                result.Severity = SeverityLevel.Medium;
                result.RiskScore = 4;
            }

            // Adjust severity based on injury keywords
            if (lowerText.Contains("injury") || lowerText.Contains("injured") || lowerText.Contains("hurt"))
            {
                result.RiskScore = Math.Min(10, result.RiskScore + 2);
                if (result.Severity == SeverityLevel.Low)
                    result.Severity = SeverityLevel.Medium;
            }

            if (lowerText.Contains("hospital") || lowerText.Contains("emergency") || lowerText.Contains("ambulance"))
            {
                result.RiskScore = Math.Min(10, result.RiskScore + 3);
                result.Severity = SeverityLevel.Critical;
            }

            // Generate summary
            result.Summary = $"Safety incident analysis identified a {result.IncidentType} incident with {result.Severity} severity level. Risk assessment score: {result.RiskScore}/10.";

            // Set confidence and key factors
            result.AnalysisConfidence = 0.75;
            result.KeyFactors = ExtractKeywords(lowerText);

            return result;
        }

        private List<string> ExtractKeywords(string text)
        {
            var keywords = new List<string>();
            var safetyKeywords = new[] { "injury", "accident", "hazard", "unsafe", "equipment", "fall", "slip", "fire", "chemical" };

            foreach (var keyword in safetyKeywords)
            {
                if (text.Contains(keyword))
                {
                    keywords.Add(keyword);
                }
            }

            return keywords.Take(5).ToList();
        }

        private List<SafetyRecommendation> GenerateBasicRecommendations(SafetyAnalysisResult analysis)
        {
            var recommendations = new List<SafetyRecommendation>();

            // Always add investigation recommendation
            recommendations.Add(new SafetyRecommendation
            {
                Type = "Administrative",
                Description = "Conduct thorough incident investigation and document findings",
                Priority = "High",
                EstimatedCost = 200,
                EstimatedTimeHours = 4,
                ResponsibleRole = "Safety Manager"
            });

            return recommendations;
        }

        private SafetyAnalysisResult CreateDefaultAnalysisResult(string message)
        {
            return new SafetyAnalysisResult
            {
                IncidentType = IncidentType.Other,
                Severity = SeverityLevel.Medium,
                RiskScore = 5,
                Summary = message,
                AnalysisConfidence = 0.5,
                KeyFactors = new List<string>(),
                Recommendations = new List<SafetyRecommendation>()
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Clean up resources if needed
                _disposed = true;
            }
        }
    }
}