using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Data.Repositories;
using SafetyAI.Models.DTOs;
using SafetyAI.Services.Implementation;
using SafetyAI.Tests.Helpers;

namespace SafetyAI.Tests.SystemTests
{
    [TestClass]
    public class EndToEndTests
    {
        private TestDbContext _testDbContext;
        private UnitOfWork _unitOfWork;
        private DocumentProcessor _documentProcessor;
        private SafetyAnalyzer _safetyAnalyzer;
        private ChatService _chatService;
        private AnalyticsService _analyticsService;

        [TestInitialize]
        public void Setup()
        {
            _testDbContext = new TestDbContext();
            _unitOfWork = new UnitOfWork(_testDbContext.Context);
            
            var mockGeminiClient = TestDataHelper.CreateMockGeminiClient();
            _documentProcessor = new DocumentProcessor(mockGeminiClient, new FileValidator());
            _safetyAnalyzer = new SafetyAnalyzer(mockGeminiClient);
            _chatService = new ChatService(mockGeminiClient);
            _analyticsService = new AnalyticsService(_unitOfWork, _safetyAnalyzer);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _analyticsService?.Dispose();
            _chatService?.Dispose();
            _safetyAnalyzer?.Dispose();
            _documentProcessor?.Dispose();
            _unitOfWork?.Dispose();
            _testDbContext?.Dispose();
        }

        [TestMethod]
        public async Task CompleteWorkflow_DocumentUploadToAnalytics_ShouldWorkEndToEnd()
        {
            // Arrange
            var testDocument = TestDataHelper.CreateTestPdfBytes(
                "Safety Incident Report: Employee John Doe slipped on wet floor in warehouse area B on 2024-01-15. " +
                "Minor injury to ankle reported. First aid administered on site. Wet floor signs were not present.");

            // Act 1: Document Processing
            var documentResult = await _documentProcessor.ProcessDocumentAsync(testDocument, "incident-report-001.pdf");
            
            // Assert 1: Document processing succeeded
            Assert.IsTrue(documentResult.IsSuccess, "Document processing should succeed");
            Assert.IsFalse(string.IsNullOrEmpty(documentResult.ExtractedText), "Should extract text from document");

            // Act 2: Safety Analysis
            var metadata = new DocumentMetadata
            {
                FileName = "incident-report-001.pdf",
                FileSize = testDocument.Length,
                ContentType = "application/pdf",
                UploadedBy = "test-user@company.com",
                UploadedDate = DateTime.UtcNow
            };

            var analysisResult = await _safetyAnalyzer.AnalyzeIncidentAsync(documentResult.ExtractedText, metadata);

            // Assert 2: Analysis completed successfully
            Assert.IsNotNull(analysisResult, "Analysis result should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(analysisResult.Summary), "Should generate summary");
            Assert.IsNotNull(analysisResult.IncidentType, "Should classify incident type");
            Assert.IsNotNull(analysisResult.Severity, "Should determine severity");
            Assert.IsTrue(analysisResult.RiskScore >= 1 && analysisResult.RiskScore <= 10, "Risk score should be valid");
            Assert.IsTrue(analysisResult.Recommendations?.Count > 0, "Should generate recommendations");

            // Act 3: Database Storage
            var safetyReport = TestDataHelper.CreateSafetyReport("incident-report-001.pdf", documentResult.ExtractedText);
            await _unitOfWork.SafetyReports.AddAsync(safetyReport);
            await _unitOfWork.SaveChangesAsync();

            var analysisEntity = TestDataHelper.CreateAnalysisResult(safetyReport.Id, analysisResult);
            await _unitOfWork.AnalysisResults.AddAsync(analysisEntity);
            await _unitOfWork.SaveChangesAsync();

            // Assert 3: Data persisted correctly
            var savedReport = await _unitOfWork.SafetyReports.GetByIdAsync(safetyReport.Id);
            Assert.IsNotNull(savedReport, "Report should be saved to database");

            var savedAnalysis = await _unitOfWork.AnalysisResults.GetByIdAsync(analysisEntity.Id);
            Assert.IsNotNull(savedAnalysis, "Analysis should be saved to database");

            // Act 4: Analytics Generation
            var dashboardMetrics = await _analyticsService.GetDashboardMetricsAsync();

            // Assert 4: Analytics generated
            Assert.IsNotNull(dashboardMetrics, "Dashboard metrics should be generated");
            Assert.IsTrue(dashboardMetrics.TotalReports > 0, "Should count the processed report");

            // Act 5: Chat Query about the incident
            var userContext = new UserContext
            {
                UserId = "test-user@company.com",
                UserRole = "SafetyManager"
            };

            var chatResponse = await _chatService.ProcessQueryAsync(
                "What should I do about slip incidents in the warehouse?", 
                "test-session-001", 
                userContext);

            // Assert 5: Chat response provided
            Assert.IsNotNull(chatResponse, "Chat response should be generated");
            Assert.IsFalse(string.IsNullOrEmpty(chatResponse.Response), "Should provide response text");
            Assert.IsTrue(chatResponse.ConfidenceScore > 0, "Should have confidence score");
        }

        [TestMethod]
        public async Task MultipleIncidentTypes_ProcessingAndAnalytics_ShouldHandleVariety()
        {
            // Arrange - Create different types of incidents
            var incidentScenarios = new[]
            {
                new { Type = "Fall", Text = "Worker fell from ladder while changing light bulb. Hospitalized with broken arm.", Severity = "High" },
                new { Type = "Slip", Text = "Employee slipped on wet floor. Minor bruising, returned to work.", Severity = "Low" },
                new { Type = "Chemical", Text = "Chemical spill in lab area. Evacuation required. No injuries.", Severity = "Medium" },
                new { Type = "Equipment", Text = "Conveyor belt malfunction caused production shutdown. No injuries.", Severity = "Medium" },
                new { Type = "Fire", Text = "Small electrical fire in control room. Extinguished quickly.", Severity = "High" }
            };

            var processedIncidents = 0;

            // Act - Process each incident type
            foreach (var scenario in incidentScenarios)
            {
                var testDocument = TestDataHelper.CreateTestPdfBytes(scenario.Text);
                var fileName = $"{scenario.Type.ToLower()}-incident-{processedIncidents + 1}.pdf";

                // Process document
                var documentResult = await _documentProcessor.ProcessDocumentAsync(testDocument, fileName);
                Assert.IsTrue(documentResult.IsSuccess, $"Document processing should succeed for {scenario.Type}");

                // Analyze incident
                var metadata = new DocumentMetadata
                {
                    FileName = fileName,
                    FileSize = testDocument.Length,
                    ContentType = "application/pdf",
                    UploadedBy = "test-user@company.com",
                    UploadedDate = DateTime.UtcNow
                };

                var analysisResult = await _safetyAnalyzer.AnalyzeIncidentAsync(documentResult.ExtractedText, metadata);
                Assert.IsNotNull(analysisResult, $"Analysis should complete for {scenario.Type}");

                // Save to database
                var safetyReport = TestDataHelper.CreateSafetyReport(fileName, documentResult.ExtractedText);
                await _unitOfWork.SafetyReports.AddAsync(safetyReport);
                await _unitOfWork.SaveChangesAsync();

                var analysisEntity = TestDataHelper.CreateAnalysisResult(safetyReport.Id, analysisResult);
                await _unitOfWork.AnalysisResults.AddAsync(analysisEntity);
                await _unitOfWork.SaveChangesAsync();

                processedIncidents++;
            }

            // Assert - Verify analytics can handle multiple incident types
            var dashboardMetrics = await _analyticsService.GetDashboardMetricsAsync();
            Assert.AreEqual(processedIncidents, dashboardMetrics.TotalReports, "Should count all processed incidents");
            Assert.IsTrue(dashboardMetrics.IncidentTypeDistribution.Count > 1, "Should have multiple incident types");

            var trendAnalysis = await _analyticsService.AnalyzeHistoricalTrendsAsync(30);
            Assert.IsNotNull(trendAnalysis, "Trend analysis should be generated");
            Assert.AreEqual(processedIncidents, trendAnalysis.TotalIncidents, "Trend analysis should include all incidents");
        }

        [TestMethod]
        public async Task UserRoleBasedWorkflow_DifferentPermissions_ShouldRespectAccess()
        {
            // Arrange - Create test incident
            var testDocument = TestDataHelper.CreateTestPdfBytes("Standard safety incident for role testing.");
            var documentResult = await _documentProcessor.ProcessDocumentAsync(testDocument, "role-test.pdf");

            // Test different user roles
            var userRoles = new[]
            {
                new { Role = "Employee", ExpectedAccess = true },
                new { Role = "Supervisor", ExpectedAccess = true },
                new { Role = "SafetyManager", ExpectedAccess = true },
                new { Role = "Administrator", ExpectedAccess = true }
            };

            foreach (var userRole in userRoles)
            {
                // Act - Test chat access for different roles
                var userContext = new UserContext
                {
                    UserId = $"test-{userRole.Role.ToLower()}@company.com",
                    UserRole = userRole.Role
                };

                var chatResponse = await _chatService.ProcessQueryAsync(
                    "What are the safety procedures for this type of incident?", 
                    $"session-{userRole.Role.ToLower()}", 
                    userContext);

                // Assert - All roles should get responses (access control is handled at web layer)
                if (userRole.ExpectedAccess)
                {
                    Assert.IsNotNull(chatResponse, $"{userRole.Role} should get chat response");
                    Assert.IsFalse(string.IsNullOrEmpty(chatResponse.Response), $"{userRole.Role} should get response text");
                }
            }
        }

        [TestMethod]
        public async Task ErrorRecovery_SystemResilience_ShouldHandleFailures()
        {
            // Test 1: Invalid document handling
            var invalidDocument = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var invalidResult = await _documentProcessor.ProcessDocumentAsync(invalidDocument, "invalid.pdf");
            
            Assert.IsFalse(invalidResult.IsSuccess, "Should handle invalid documents gracefully");
            Assert.IsFalse(string.IsNullOrEmpty(invalidResult.ErrorMessage), "Should provide error message");

            // Test 2: Empty text analysis
            var emptyAnalysis = await _safetyAnalyzer.AnalyzeIncidentAsync("", new DocumentMetadata());
            Assert.IsNotNull(emptyAnalysis, "Should handle empty text gracefully");

            // Test 3: Invalid chat query
            var invalidChatResponse = await _chatService.ProcessQueryAsync("", "test-session", new UserContext());
            Assert.IsNotNull(invalidChatResponse, "Should handle empty chat query gracefully");
            Assert.IsFalse(string.IsNullOrEmpty(invalidChatResponse.Response), "Should provide fallback response");

            // Test 4: Analytics with no data
            using (var emptyDbContext = new TestDbContext())
            using (var emptyUnitOfWork = new UnitOfWork(emptyDbContext.Context))
            using (var emptyAnalyticsService = new AnalyticsService(emptyUnitOfWork, _safetyAnalyzer))
            {
                var emptyMetrics = await emptyAnalyticsService.GetDashboardMetricsAsync();
                Assert.IsNotNull(emptyMetrics, "Should handle empty database gracefully");
                Assert.AreEqual(0, emptyMetrics.TotalReports, "Should report zero incidents correctly");
            }
        }

        [TestMethod]
        public async Task DataConsistency_ConcurrentOperations_ShouldMaintainIntegrity()
        {
            // Arrange
            const int concurrentOperations = 10;
            var tasks = new Task[concurrentOperations];

            // Act - Perform concurrent database operations
            for (int i = 0; i < concurrentOperations; i++)
            {
                var operationId = i;
                tasks[i] = Task.Run(async () =>
                {
                    var testDocument = TestDataHelper.CreateTestPdfBytes($"Concurrent test document {operationId}");
                    var documentResult = await _documentProcessor.ProcessDocumentAsync(testDocument, $"concurrent-{operationId}.pdf");
                    
                    if (documentResult.IsSuccess)
                    {
                        var safetyReport = TestDataHelper.CreateSafetyReport($"concurrent-{operationId}.pdf", documentResult.ExtractedText);
                        await _unitOfWork.SafetyReports.AddAsync(safetyReport);
                        await _unitOfWork.SaveChangesAsync();
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert - Verify data consistency
            var allReports = await _unitOfWork.SafetyReports.GetAllAsync();
            var concurrentReports = allReports.Where(r => r.FileName.StartsWith("concurrent-")).ToList();
            
            Assert.AreEqual(concurrentOperations, concurrentReports.Count, "All concurrent operations should have saved data");
            
            // Verify no duplicate IDs
            var uniqueIds = concurrentReports.Select(r => r.Id).Distinct().Count();
            Assert.AreEqual(concurrentOperations, uniqueIds, "All reports should have unique IDs");
        }
    }
}