using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Services.Implementation;
using SafetyAI.Services.Interfaces;
using SafetyAI.Models.DTOs;
using SafetyAI.Models.Enums;
using SafetyAI.Tests.Helpers;

namespace SafetyAI.Tests.Validation
{
    [TestClass]
    public class AIModelValidationTests
    {
        private ISafetyAnalyzer _safetyAnalyzer;
        private IChatService _chatService;

        [TestInitialize]
        public void Setup()
        {
            var geminiClient = new MockGeminiAPIClient();
            _safetyAnalyzer = new SafetyAnalyzer(geminiClient);
            _chatService = new ChatService(geminiClient);
        }

        [TestMethod]
        public async Task IncidentClassification_KnownScenarios_MeetsAccuracyTarget()
        {
            // Arrange - Test cases with expected classifications
            var testCases = new[]
            {
                new { Text = "Employee slipped on wet floor in warehouse area", ExpectedType = IncidentType.Slip, ExpectedSeverity = SeverityLevel.Medium },
                new { Text = "Worker fell from ladder while changing light bulb", ExpectedType = IncidentType.Fall, ExpectedSeverity = SeverityLevel.High },
                new { Text = "Conveyor belt motor overheated and stopped working", ExpectedType = IncidentType.EquipmentFailure, ExpectedSeverity = SeverityLevel.Medium },
                new { Text = "Chemical spill in laboratory, no injuries reported", ExpectedType = IncidentType.ChemicalExposure, ExpectedSeverity = SeverityLevel.High },
                new { Text = "Fire alarm activated due to smoke in kitchen area", ExpectedType = IncidentType.Fire, ExpectedSeverity = SeverityLevel.Critical },
                new { Text = "Near miss: forklift almost hit pedestrian in loading dock", ExpectedType = IncidentType.NearMiss, ExpectedSeverity = SeverityLevel.Medium }
            };

            var correctClassifications = 0;
            var correctSeverityAssessments = 0;

            // Act & Assert
            foreach (var testCase in testCases)
            {
                var metadata = new DocumentMetadata
                {
                    FileName = "test-scenario.txt",
                    UploadedBy = "test-validator",
                    UploadedDate = DateTime.UtcNow
                };

                var result = await _safetyAnalyzer.AnalyzeIncidentAsync(testCase.Text, metadata);

                // Check incident type classification
                if (result.IncidentType == testCase.ExpectedType)
                {
                    correctClassifications++;
                }

                // Check severity assessment (allow one level tolerance)
                var actualSeverity = result.Severity ?? SeverityLevel.Medium;
                var severityDifference = Math.Abs((int)actualSeverity - (int)testCase.ExpectedSeverity);
                if (severityDifference <= 1)
                {
                    correctSeverityAssessments++;
                }

                // Validate other requirements
                Assert.IsTrue(result.AnalysisConfidence > 0.5, $"Confidence should be reasonable for: {testCase.Text}");
                Assert.IsTrue(result.RiskScore >= 1 && result.RiskScore <= 10, $"Risk score should be valid for: {testCase.Text}");
                Assert.IsNotNull(result.Summary, $"Summary should be provided for: {testCase.Text}");
                Assert.IsTrue(result.Recommendations?.Count > 0, $"Recommendations should be provided for: {testCase.Text}");
            }

            // Assert accuracy targets
            var classificationAccuracy = (double)correctClassifications / testCases.Length * 100;
            var severityAccuracy = (double)correctSeverityAssessments / testCases.Length * 100;

            Assert.IsTrue(classificationAccuracy >= 95, 
                $"Incident classification accuracy should be >= 95%, got {classificationAccuracy:F1}%");
            Assert.IsTrue(severityAccuracy >= 90, 
                $"Severity assessment accuracy should be >= 90%, got {severityAccuracy:F1}%");
        }

        [TestMethod]
        public async Task BiasDetection_DiverseScenarios_ShowsNoSystematicBias()
        {
            // Arrange - Test cases designed to detect potential biases
            var testCases = new[]
            {
                // Gender bias test
                new { Text = "Male employee injured in machinery accident", Category = "Gender" },
                new { Text = "Female employee injured in machinery accident", Category = "Gender" },
                
                // Age bias test
                new { Text = "Young worker slipped on wet floor", Category = "Age" },
                new { Text = "Experienced worker slipped on wet floor", Category = "Age" },
                
                // Department bias test
                new { Text = "Office worker reported ergonomic injury", Category = "Department" },
                new { Text = "Factory worker reported ergonomic injury", Category = "Department" },
                
                // Shift bias test
                new { Text = "Day shift incident in production area", Category = "Shift" },
                new { Text = "Night shift incident in production area", Category = "Shift" }
            };

            var resultsByCategory = new Dictionary<string, List<SafetyAnalysisResult>>();

            // Act
            foreach (var testCase in testCases)
            {
                var metadata = new DocumentMetadata
                {
                    FileName = "bias-test.txt",
                    UploadedBy = "bias-validator",
                    UploadedDate = DateTime.UtcNow
                };

                var result = await _safetyAnalyzer.AnalyzeIncidentAsync(testCase.Text, metadata);

                if (!resultsByCategory.ContainsKey(testCase.Category))
                {
                    resultsByCategory[testCase.Category] = new List<SafetyAnalysisResult>();
                }
                resultsByCategory[testCase.Category].Add(result);
            }

            // Assert - Check for systematic bias
            foreach (var category in resultsByCategory.Keys)
            {
                var results = resultsByCategory[category];
                if (results.Count >= 2)
                {
                    var riskScores = results.Select(r => r.RiskScore).ToList();
                    var avgRiskScore = riskScores.Average();
                    var riskScoreVariance = riskScores.Select(score => Math.Pow(score - avgRiskScore, 2)).Average();

                    // Risk scores within same category should not vary dramatically (indicating bias)
                    Assert.IsTrue(riskScoreVariance < 4, 
                        $"Risk score variance in {category} category should be low, got {riskScoreVariance:F2}");

                    var confidenceScores = results.Select(r => r.AnalysisConfidence).ToList();
                    var avgConfidence = confidenceScores.Average();
                    var confidenceVariance = confidenceScores.Select(conf => Math.Pow(conf - avgConfidence, 2)).Average();

                    Assert.IsTrue(confidenceVariance < 0.1, 
                        $"Confidence variance in {category} category should be low, got {confidenceVariance:F3}");
                }
            }
        }

        [TestMethod]
        public async Task ChatService_SafetyKnowledge_ProvidesAccurateInformation()
        {
            // Arrange - Safety knowledge validation questions
            var knowledgeTests = new[]
            {
                new { 
                    Question = "What is the OSHA standard for fall protection?", 
                    ExpectedKeywords = new[] { "29 CFR 1926.501", "fall protection", "6 feet" }
                },
                new { 
                    Question = "What PPE is required for chemical handling?", 
                    ExpectedKeywords = new[] { "gloves", "goggles", "respirator", "chemical resistant" }
                },
                new { 
                    Question = "What are the steps in lockout/tagout procedure?", 
                    ExpectedKeywords = new[] { "shutdown", "isolate", "lockout", "tagout", "verify" }
                },
                new { 
                    Question = "How often should safety training be conducted?", 
                    ExpectedKeywords = new[] { "annual", "training", "refresher", "new employee" }
                }
            };

            var userContext = new UserContext
            {
                UserId = "knowledge-validator",
                UserRole = "SafetyManager"
            };

            // Act & Assert
            foreach (var test in knowledgeTests)
            {
                var response = await _chatService.ProcessQueryAsync(test.Question, "knowledge-session", userContext);

                Assert.IsNotNull(response.Response, $"Response should be provided for: {test.Question}");
                Assert.IsTrue(response.ConfidenceScore > 0.7, 
                    $"Confidence should be high for knowledge questions, got {response.ConfidenceScore:F2} for: {test.Question}");

                var responseLower = response.Response.ToLowerInvariant();
                var keywordMatches = test.ExpectedKeywords.Count(keyword => 
                    responseLower.Contains(keyword.ToLowerInvariant()));

                Assert.IsTrue(keywordMatches >= test.ExpectedKeywords.Length / 2, 
                    $"Response should contain relevant keywords for: {test.Question}. Found {keywordMatches}/{test.ExpectedKeywords.Length} keywords");
            }
        }

        [TestMethod]
        public async Task ModelConsistency_RepeatedQueries_ProducesStableResults()
        {
            // Arrange
            var testText = "Worker injured hand while operating machinery without proper guards";
            var metadata = new DocumentMetadata
            {
                FileName = "consistency-test.txt",
                UploadedBy = "consistency-validator",
                UploadedDate = DateTime.UtcNow
            };

            var results = new List<SafetyAnalysisResult>();

            // Act - Run same analysis multiple times
            for (int i = 0; i < 5; i++)
            {
                var result = await _safetyAnalyzer.AnalyzeIncidentAsync(testText, metadata);
                results.Add(result);
            }

            // Assert - Check consistency
            var incidentTypes = results.Select(r => r.IncidentType).Distinct().Count();
            var severityLevels = results.Select(r => r.Severity).Distinct().Count();
            var riskScores = results.Select(r => r.RiskScore).ToList();
            var avgRiskScore = riskScores.Average();
            var riskScoreVariance = riskScores.Select(score => Math.Pow(score - avgRiskScore, 2)).Average();

            Assert.IsTrue(incidentTypes <= 2, "Incident type should be consistent across runs");
            Assert.IsTrue(severityLevels <= 2, "Severity level should be consistent across runs");
            Assert.IsTrue(riskScoreVariance < 1, $"Risk score should be stable, variance: {riskScoreVariance:F2}");

            // All results should have reasonable confidence
            Assert.IsTrue(results.All(r => r.AnalysisConfidence > 0.6), 
                "All repeated analyses should have reasonable confidence");
        }

        [TestMethod]
        public async Task EdgeCases_UnusualInputs_HandlesGracefully()
        {
            // Arrange - Edge case scenarios
            var edgeCases = new[]
            {
                "", // Empty string
                "a", // Single character
                new string('x', 10000), // Very long string
                "Normal incident report with some émojis 🚨⚠️🔥", // Unicode characters
                "INCIDENT REPORT ALL CAPS SHOUTING", // All caps
                "incident report all lowercase no punctuation", // No punctuation
                "Mixed languages: Incident occurred בבטיחות العمل безопасность", // Multiple languages
                "123456789 numbers only incident", // Mostly numbers
                "Special chars: @#$%^&*()_+ incident occurred", // Special characters
            };

            var metadata = new DocumentMetadata
            {
                FileName = "edge-case-test.txt",
                UploadedBy = "edge-case-validator",
                UploadedDate = DateTime.UtcNow
            };

            // Act & Assert
            foreach (var edgeCase in edgeCases)
            {
                var result = await _safetyAnalyzer.AnalyzeIncidentAsync(edgeCase, metadata);

                Assert.IsNotNull(result, $"Result should not be null for edge case: {edgeCase.Substring(0, Math.Min(50, edgeCase.Length))}...");
                Assert.IsTrue(result.RiskScore >= 1 && result.RiskScore <= 10, 
                    $"Risk score should be valid for edge case: {edgeCase.Substring(0, Math.Min(50, edgeCase.Length))}...");
                Assert.IsTrue(result.AnalysisConfidence >= 0, 
                    $"Confidence should be non-negative for edge case: {edgeCase.Substring(0, Math.Min(50, edgeCase.Length))}...");
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _safetyAnalyzer?.Dispose();
            _chatService?.Dispose();
        }
    }
}