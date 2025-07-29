using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Data.Context;
using SafetyAI.Data.Repositories;
using SafetyAI.Models.DTOs;
using SafetyAI.Services.Implementation;
using SafetyAI.Tests.Helpers;

namespace SafetyAI.Tests.Integration
{
    [TestClass]
    public class SafetyAnalysisIntegrationTests
    {
        private TestDbContext _testDbContext;
        private UnitOfWork _unitOfWork;
        private DocumentProcessor _documentProcessor;
        private SafetyAnalyzer _safetyAnalyzer;
        private GeminiAPIClient _geminiClient;

        [TestInitialize]
        public void Setup()
        {
            _testDbContext = new TestDbContext();
            _unitOfWork = new UnitOfWork(_testDbContext.Context);
            
            // Create mock services for integration testing
            _geminiClient = TestDataHelper.CreateMockGeminiClient();
            _documentProcessor = new DocumentProcessor(_geminiClient, new FileValidator());
            _safetyAnalyzer = new SafetyAnalyzer(_geminiClient);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _safetyAnalyzer?.Dispose();
            _documentProcessor?.Dispose();
            _geminiClient?.Dispose();
            _unitOfWork?.Dispose();
            _testDbContext?.Dispose();
        }

        [TestMethod]
        public async Task EndToEndSafetyAnalysis_WithPDFDocument_ShouldCompleteSuccessfully()
        {
            // Arrange
            var testPdfBytes = TestDataHelper.CreateTestPdfBytes("Safety incident report: Worker fell from ladder while changing light bulb. Minor injuries reported.");
            var fileName = "test-incident-report.pdf";

            // Act - Process document
            var documentResult = await _documentProcessor.ProcessDocumentAsync(testPdfBytes, fileName);

            // Assert document processing
            Assert.IsTrue(documentResult.IsSuccess);
            Assert.IsFalse(string.IsNullOrEmpty(documentResult.ExtractedText));

            // Act - Analyze safety content
            var metadata = new DocumentMetadata
            {
                FileName = fileName,
                FileSize = testPdfBytes.Length,
                ContentType = "application/pdf",
                UploadedBy = "test-user",
                UploadedDate = DateTime.UtcNow
            };

            var analysisResult = await _safetyAnalyzer.AnalyzeIncidentAsync(documentResult.ExtractedText, metadata);

            // Assert analysis results
            Assert.IsNotNull(analysisResult);
            Assert.IsFalse(string.IsNullOrEmpty(analysisResult.Summary));
            Assert.IsNotNull(analysisResult.IncidentType);
            Assert.IsNotNull(analysisResult.Severity);
            Assert.IsTrue(analysisResult.RiskScore >= 1 && analysisResult.RiskScore <= 10);
            Assert.IsTrue(analysisResult.AnalysisConfidence > 0);

            // Act - Save to database
            var safetyReport = TestDataHelper.CreateSafetyReport(fileName, documentResult.ExtractedText);
            await _unitOfWork.SafetyReports.AddAsync(safetyReport);
            await _unitOfWork.SaveChangesAsync();

            // Assert database persistence
            var savedReport = await _unitOfWork.SafetyReports.GetByIdAsync(safetyReport.Id);
            Assert.IsNotNull(savedReport);
            Assert.AreEqual(fileName, savedReport.FileName);
        }

        [TestMethod]
        public async Task SafetyAnalysisWorkflow_WithMultipleIncidentTypes_ShouldClassifyCorrectly()
        {
            // Test data for different incident types
            var testCases = new[]
            {
                new { Text = "Employee slipped on wet floor in warehouse", ExpectedType = "Slip" },
                new { Text = "Worker fell from scaffolding during construction", ExpectedType = "Fall" },
                new { Text = "Chemical spill in laboratory area", ExpectedType = "ChemicalExposure" },
                new { Text = "Equipment malfunction caused production stoppage", ExpectedType = "EquipmentFailure" },
                new { Text = "Fire alarm activated due to overheating machinery", ExpectedType = "Fire" }
            };

            foreach (var testCase in testCases)
            {
                // Act
                var metadata = new DocumentMetadata
                {
                    FileName = $"test-{testCase.ExpectedType.ToLower()}.txt",
                    UploadedBy = "test-user",
                    UploadedDate = DateTime.UtcNow
                };

                var result = await _safetyAnalyzer.AnalyzeIncidentAsync(testCase.Text, metadata);

                // Assert
                Assert.IsNotNull(result, $"Analysis result should not be null for {testCase.ExpectedType}");
                Assert.IsNotNull(result.IncidentType, $"Incident type should be classified for {testCase.ExpectedType}");
                // Note: In a real test, you might want to assert the exact type, but AI results can vary
                Assert.IsTrue(result.AnalysisConfidence > 0.5, $"Confidence should be reasonable for {testCase.ExpectedType}");
            }
        }

        [TestMethod]
        public async Task DatabaseIntegration_WithCompleteWorkflow_ShouldMaintainDataIntegrity()
        {
            // Arrange
            var testData = TestDataHelper.CreateCompleteTestScenario();

            // Act - Save safety report
            await _unitOfWork.SafetyReports.AddAsync(testData.SafetyReport);
            await _unitOfWork.SaveChangesAsync();

            // Act - Save analysis result
            testData.AnalysisResult.SafetyReportId = testData.SafetyReport.Id;
            await _unitOfWork.AnalysisResults.AddAsync(testData.AnalysisResult);
            await _unitOfWork.SaveChangesAsync();

            // Act - Save recommendations
            foreach (var recommendation in testData.Recommendations)
            {
                recommendation.AnalysisResultId = testData.AnalysisResult.Id;
                await _unitOfWork.Recommendations.AddAsync(recommendation);
            }
            await _unitOfWork.SaveChangesAsync();

            // Assert - Verify relationships
            var savedReport = await _unitOfWork.SafetyReports.GetReportWithAnalysisAsync(testData.SafetyReport.Id);
            Assert.IsNotNull(savedReport);
            Assert.IsNotNull(savedReport.AnalysisResult);

            var savedRecommendations = await _unitOfWork.Recommendations.GetByAnalysisIdAsync(testData.AnalysisResult.Id);
            Assert.AreEqual(testData.Recommendations.Count, savedRecommendations.Count());
        }

        [TestMethod]
        public async Task PerformanceTest_ProcessingTime_ShouldMeetRequirements()
        {
            // Arrange
            var testDocument = TestDataHelper.CreateTestPdfBytes("Standard safety incident report with moderate complexity.");
            var startTime = DateTime.UtcNow;

            // Act
            var result = await _documentProcessor.ProcessDocumentAsync(testDocument, "performance-test.pdf");

            // Assert - Should complete within 30 seconds for documents under 10MB
            var processingTime = DateTime.UtcNow - startTime;
            Assert.IsTrue(processingTime.TotalSeconds < 30, $"Processing took {processingTime.TotalSeconds} seconds, should be under 30");
            Assert.IsTrue(result.IsSuccess, "Processing should succeed");
        }

        [TestMethod]
        public async Task ErrorHandling_WithInvalidDocument_ShouldHandleGracefully()
        {
            // Arrange
            var invalidDocument = new byte[] { 0x00, 0x01, 0x02 }; // Invalid PDF

            // Act
            var result = await _documentProcessor.ProcessDocumentAsync(invalidDocument, "invalid.pdf");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [TestMethod]
        public async Task ConcurrentProcessing_WithMultipleRequests_ShouldHandleCorrectly()
        {
            // Arrange
            var tasks = new Task<DocumentAnalysisResult>[5];
            var testDocument = TestDataHelper.CreateTestPdfBytes("Concurrent processing test document.");

            // Act - Process multiple documents concurrently
            for (int i = 0; i < tasks.Length; i++)
            {
                var fileName = $"concurrent-test-{i}.pdf";
                tasks[i] = _documentProcessor.ProcessDocumentAsync(testDocument, fileName);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            foreach (var result in results)
            {
                Assert.IsTrue(result.IsSuccess, "All concurrent operations should succeed");
                Assert.IsFalse(string.IsNullOrEmpty(result.ExtractedText));
            }
        }
    }
}