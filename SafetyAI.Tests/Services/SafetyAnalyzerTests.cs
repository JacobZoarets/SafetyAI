using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Services.Implementation;
using SafetyAI.Services.Interfaces;
using SafetyAI.Models.DTOs;
using SafetyAI.Models.Enums;

namespace SafetyAI.Tests.Services
{
    [TestClass]
    public class SafetyAnalyzerTests
    {
        private SafetyAnalyzer _safetyAnalyzer;
        private MockGeminiAPIClient _mockGeminiClient;

        [TestInitialize]
        public void Setup()
        {
            _mockGeminiClient = new MockGeminiAPIClient();
            _safetyAnalyzer = new SafetyAnalyzer(_mockGeminiClient);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _safetyAnalyzer?.Dispose();
        }

        [TestMethod]
        public async Task AnalyzeIncidentAsync_WithValidText_ShouldReturnAnalysisResult()
        {
            // Arrange
            var extractedText = "Employee slipped on wet floor in warehouse area. Minor injury to ankle. First aid administered on site.";
            var metadata = new DocumentMetadata
            {
                FileName = "incident_report.pdf",
                UploadedBy = "john.doe@company.com"
            };

            var mockAnalysis = new SafetyAnalysisResult
            {
                IncidentType = IncidentType.Slip,
                Severity = SeverityLevel.Medium,
                RiskScore = 5,
                Summary = "Slip incident with minor injury",
                AnalysisConfidence = 0.92
            };

            _mockGeminiClient.SetupSafetyAnalysisResponse(mockAnalysis);

            // Act
            var result = await _safetyAnalyzer.AnalyzeIncidentAsync(extractedText, metadata);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(IncidentType.Slip, result.IncidentType);
            Assert.AreEqual(SeverityLevel.Medium, result.Severity);
            Assert.AreEqual(5, result.RiskScore);
            Assert.IsTrue(result.AnalysisConfidence > 0.8);
        }

        [TestMethod]
        public async Task AnalyzeIncidentAsync_WithFallIncident_ShouldClassifyCorrectly()
        {
            // Arrange
            var extractedText = "Worker fell from ladder while changing light bulb. Serious injury to back. Ambulance called.";
            var metadata = new DocumentMetadata { FileName = "fall_incident.pdf" };

            var mockAnalysis = new SafetyAnalysisResult
            {
                IncidentType = IncidentType.Fall,
                Severity = SeverityLevel.High,
                RiskScore = 8,
                Summary = "Fall from height with serious injury",
                AnalysisConfidence = 0.95
            };

            _mockGeminiClient.SetupSafetyAnalysisResponse(mockAnalysis);

            // Act
            var result = await _safetyAnalyzer.AnalyzeIncidentAsync(extractedText, metadata);

            // Assert
            Assert.AreEqual(IncidentType.Fall, result.IncidentType);
            Assert.AreEqual(SeverityLevel.High, result.Severity);
            Assert.IsTrue(result.RiskScore >= 7);
        }

        [TestMethod]
        public async Task AnalyzeIncidentAsync_WithEquipmentFailure_ShouldClassifyCorrectly()
        {
            // Arrange
            var extractedText = "Conveyor belt motor overheated causing production stoppage. No injuries reported. Equipment shut down for inspection.";
            var metadata = new DocumentMetadata { FileName = "equipment_failure.pdf" };

            var mockAnalysis = new SafetyAnalysisResult
            {
                IncidentType = IncidentType.EquipmentFailure,
                Severity = SeverityLevel.Medium,
                RiskScore = 6,
                Summary = "Equipment failure with production impact",
                AnalysisConfidence = 0.88
            };

            _mockGeminiClient.SetupSafetyAnalysisResponse(mockAnalysis);

            // Act
            var result = await _safetyAnalyzer.AnalyzeIncidentAsync(extractedText, metadata);

            // Assert
            Assert.AreEqual(IncidentType.EquipmentFailure, result.IncidentType);
            Assert.AreEqual(SeverityLevel.Medium, result.Severity);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AnalyzeIncidentAsync_WithNullText_ShouldThrowException()
        {
            // Arrange
            string extractedText = null;
            var metadata = new DocumentMetadata { FileName = "test.pdf" };

            // Act
            await _safetyAnalyzer.AnalyzeIncidentAsync(extractedText, metadata);
        }

        [TestMethod]
        public async Task AnalyzeIncidentAsync_WithEmptyText_ShouldReturnFailureResult()
        {
            // Arrange
            var extractedText = "";
            var metadata = new DocumentMetadata { FileName = "empty.pdf" };

            // Act
            var result = await _safetyAnalyzer.AnalyzeIncidentAsync(extractedText, metadata);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0.0, result.AnalysisConfidence);
            Assert.IsTrue(result.Summary.Contains("failed"));
        }

        [TestMethod]
        public async Task GenerateRecommendationsAsync_WithFallIncident_ShouldReturnFallRecommendations()
        {
            // Arrange
            var analysis = new SafetyAnalysisResult
            {
                IncidentType = IncidentType.Fall,
                Severity = SeverityLevel.High,
                RiskScore = 8
            };

            // Act
            var recommendations = await _safetyAnalyzer.GenerateRecommendationsAsync(analysis);

            // Assert
            Assert.IsNotNull(recommendations);
            Assert.IsTrue(recommendations.Count > 0);
            Assert.IsTrue(recommendations.Any(r => r.Description.ToLower().Contains("fall protection")));
            Assert.IsTrue(recommendations.Any(r => r.Priority == "Critical" || r.Priority == "High"));
        }

        [TestMethod]
        public async Task GenerateRecommendationsAsync_WithSlipIncident_ShouldReturnSlipRecommendations()
        {
            // Arrange
            var analysis = new SafetyAnalysisResult
            {
                IncidentType = IncidentType.Slip,
                Severity = SeverityLevel.Medium,
                RiskScore = 5
            };

            // Act
            var recommendations = await _safetyAnalyzer.GenerateRecommendationsAsync(analysis);

            // Assert
            Assert.IsNotNull(recommendations);
            Assert.IsTrue(recommendations.Count > 0);
            Assert.IsTrue(recommendations.Any(r => r.Description.ToLower().Contains("slip") || r.Description.ToLower().Contains("floor")));
        }

        [TestMethod]
        public async Task MapToStandardsAsync_WithFallIncident_ShouldReturnOSHAStandards()
        {
            // Arrange
            var analysis = new SafetyAnalysisResult
            {
                IncidentType = IncidentType.Fall,
                Severity = SeverityLevel.High,
                RiskScore = 8
            };

            // Act
            var mapping = await _safetyAnalyzer.MapToStandardsAsync(analysis);

            // Assert
            Assert.IsNotNull(mapping);
            Assert.IsNotNull(mapping.OSHAStandards);
            Assert.IsTrue(mapping.OSHAStandards.Count > 0);
            Assert.IsTrue(mapping.OSHAStandards.Any(s => s.Contains("Fall Protection")));
            Assert.IsTrue(mapping.OSHAStandards.Any(s => s.Contains("General Duty Clause")));
        }

        [TestMethod]
        public async Task MapToStandardsAsync_WithChemicalExposure_ShouldReturnChemicalStandards()
        {
            // Arrange
            var analysis = new SafetyAnalysisResult
            {
                IncidentType = IncidentType.ChemicalExposure,
                Severity = SeverityLevel.High,
                RiskScore = 7
            };

            // Act
            var mapping = await _safetyAnalyzer.MapToStandardsAsync(analysis);

            // Assert
            Assert.IsNotNull(mapping);
            Assert.IsTrue(mapping.OSHAStandards.Any(s => s.Contains("Hazard Communication")));
            Assert.IsTrue(mapping.ISO45001Requirements.Count > 0);
        }

        [TestMethod]
        public async Task AnalyzeTrendsAsync_WithMultipleIncidents_ShouldReturnTrendAnalysis()
        {
            // Arrange
            var historicalAnalyses = new List<SafetyAnalysisResult>
            {
                new SafetyAnalysisResult { IncidentType = IncidentType.Slip, Severity = SeverityLevel.Medium, RiskScore = 5 },
                new SafetyAnalysisResult { IncidentType = IncidentType.Slip, Severity = SeverityLevel.Low, RiskScore = 3 },
                new SafetyAnalysisResult { IncidentType = IncidentType.Fall, Severity = SeverityLevel.High, RiskScore = 8 },
                new SafetyAnalysisResult { IncidentType = IncidentType.EquipmentFailure, Severity = SeverityLevel.Medium, RiskScore = 6 },
                new SafetyAnalysisResult { IncidentType = IncidentType.Slip, Severity = SeverityLevel.Medium, RiskScore = 4 }
            };

            // Act
            var trendAnalysis = await _safetyAnalyzer.AnalyzeTrendsAsync(historicalAnalyses);

            // Assert
            Assert.IsNotNull(trendAnalysis);
            Assert.AreEqual(5, trendAnalysis.TotalIncidents);
            Assert.IsTrue(trendAnalysis.AverageRiskScore > 0);
            Assert.IsTrue(trendAnalysis.IncidentTypeTrends.ContainsKey(IncidentType.Slip));
            Assert.AreEqual(3, trendAnalysis.IncidentTypeTrends[IncidentType.Slip]);
            Assert.IsTrue(trendAnalysis.IdentifiedPatterns.Count >= 0);
        }

        [TestMethod]
        public async Task AnalyzeTrendsAsync_WithHighSlipFrequency_ShouldIdentifyPattern()
        {
            // Arrange
            var historicalAnalyses = new List<SafetyAnalysisResult>();
            
            // Create 7 slip incidents out of 10 total (70%)
            for (int i = 0; i < 7; i++)
            {
                historicalAnalyses.Add(new SafetyAnalysisResult 
                { 
                    IncidentType = IncidentType.Slip, 
                    Severity = SeverityLevel.Medium, 
                    RiskScore = 5 
                });
            }
            
            for (int i = 0; i < 3; i++)
            {
                historicalAnalyses.Add(new SafetyAnalysisResult 
                { 
                    IncidentType = IncidentType.Fall, 
                    Severity = SeverityLevel.Low, 
                    RiskScore = 3 
                });
            }

            // Act
            var trendAnalysis = await _safetyAnalyzer.AnalyzeTrendsAsync(historicalAnalyses);

            // Assert
            Assert.IsNotNull(trendAnalysis);
            Assert.IsTrue(trendAnalysis.IdentifiedPatterns.Any(p => p.Contains("Slip")));
            Assert.IsTrue(trendAnalysis.TrendRecommendations.Any(r => r.Description.Contains("Slip")));
        }

        [TestMethod]
        public async Task AnalyzeTrendsAsync_WithIncreasingRiskTrend_ShouldRecommendReview()
        {
            // Arrange - Create incidents with increasing risk scores over time
            var historicalAnalyses = new List<SafetyAnalysisResult>
            {
                new SafetyAnalysisResult { IncidentType = IncidentType.Slip, RiskScore = 3 },
                new SafetyAnalysisResult { IncidentType = IncidentType.Fall, RiskScore = 4 },
                new SafetyAnalysisResult { IncidentType = IncidentType.Slip, RiskScore = 6 },
                new SafetyAnalysisResult { IncidentType = IncidentType.Fall, RiskScore = 7 },
                new SafetyAnalysisResult { IncidentType = IncidentType.EquipmentFailure, RiskScore = 8 },
                new SafetyAnalysisResult { IncidentType = IncidentType.Fall, RiskScore = 9 }
            };

            // Act
            var trendAnalysis = await _safetyAnalyzer.AnalyzeTrendsAsync(historicalAnalyses);

            // Assert
            Assert.IsNotNull(trendAnalysis);
            Assert.AreEqual("Increasing", trendAnalysis.RiskScoreTrend);
            Assert.IsTrue(trendAnalysis.TrendRecommendations.Any(r => 
                r.Description.Contains("comprehensive safety program review") && 
                r.Priority == "Critical"));
        }

        [TestMethod]
        public async Task AnalyzeTrendsAsync_WithEmptyList_ShouldReturnBasicAnalysis()
        {
            // Arrange
            var historicalAnalyses = new List<SafetyAnalysisResult>();

            // Act
            var trendAnalysis = await _safetyAnalyzer.AnalyzeTrendsAsync(historicalAnalyses);

            // Assert
            Assert.IsNotNull(trendAnalysis);
            Assert.AreEqual(0, trendAnalysis.TotalIncidents);
            Assert.AreEqual(0, trendAnalysis.AverageRiskScore);
        }
    }

    // Extension of MockGeminiAPIClient to support safety analysis
    public partial class MockGeminiAPIClient
    {
        private SafetyAnalysisResult _mockSafetyAnalysis;

        public void SetupSafetyAnalysisResponse(SafetyAnalysisResult analysisResult)
        {
            _mockSafetyAnalysis = analysisResult;
        }

        public new async Task<SafetyAnalysisResult> AnalyzeSafetyContentAsync(string text)
        {
            await Task.Delay(100); // Simulate processing time
            
            if (_shouldFail)
            {
                throw new SafetyAI.Services.Exceptions.GeminiAPIException(_failureMessage);
            }
            
            return _mockSafetyAnalysis ?? new SafetyAnalysisResult
            {
                IncidentType = IncidentType.Other,
                Severity = SeverityLevel.Medium,
                RiskScore = 5,
                Summary = "Mock analysis result",
                AnalysisConfidence = 0.9
            };
        }
    }
}