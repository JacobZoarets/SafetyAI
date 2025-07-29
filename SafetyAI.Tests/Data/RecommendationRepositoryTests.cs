using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Data.Repositories;
using SafetyAI.Models.Entities;
using SafetyAI.Models.Enums;
using SafetyAI.Tests.Helpers;

namespace SafetyAI.Tests.Data
{
    [TestClass]
    public class RecommendationRepositoryTests
    {
        private TestDbContext _context;
        private RecommendationRepository _repository;
        private SafetyReportRepository _reportRepository;
        private AnalysisResultRepository _analysisRepository;

        [TestInitialize]
        public void Setup()
        {
            _context = new TestDbContext();
            _repository = new RecommendationRepository(_context);
            _reportRepository = new SafetyReportRepository(_context);
            _analysisRepository = new AnalysisResultRepository(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
            _repository?.Dispose();
            _reportRepository?.Dispose();
            _analysisRepository?.Dispose();
        }

        [TestMethod]
        public async Task AddAsync_ValidRecommendation_ShouldAddToDatabase()
        {
            // Arrange
            var report = new SafetyReport
            {
                FileName = "test_report.pdf",
                UploadedBy = "test@example.com"
            };

            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            var analysis = new AnalysisResult
            {
                ReportId = report.Id,
                IncidentType = "Slip",
                Severity = "Medium",
                RiskScore = 5
            };

            await _analysisRepository.AddAsync(analysis);
            await _analysisRepository.SaveChangesAsync();

            var recommendation = new Recommendation
            {
                AnalysisId = analysis.Id,
                RecommendationType = "Preventive",
                Description = "Install wet floor warning signs",
                Priority = "High",
                EstimatedCost = 250.00m,
                EstimatedTimeHours = 4,
                ResponsibleRole = "Facility Manager"
            };

            // Act
            await _repository.AddAsync(recommendation);
            await _repository.SaveChangesAsync();

            // Assert
            var savedRecommendation = await _repository.GetByIdAsync(recommendation.Id);
            Assert.IsNotNull(savedRecommendation);
            Assert.AreEqual("Preventive", savedRecommendation.RecommendationType);
            Assert.AreEqual("High", savedRecommendation.Priority);
            Assert.AreEqual(RecommendationStatus.Pending, savedRecommendation.Status);
        }

        [TestMethod]
        public async Task GetRecommendationsByStatusAsync_WithPendingStatus_ShouldReturnPendingRecommendations()
        {
            // Arrange
            var report = new SafetyReport { FileName = "test_report.pdf", UploadedBy = "test@example.com" };
            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            var analysis = new AnalysisResult { ReportId = report.Id, IncidentType = "Slip", Severity = "Medium", RiskScore = 5 };
            await _analysisRepository.AddAsync(analysis);
            await _analysisRepository.SaveChangesAsync();

            var pendingRecommendation = new Recommendation
            {
                AnalysisId = analysis.Id,
                Description = "Pending recommendation",
                Status = RecommendationStatus.Pending
            };

            var completedRecommendation = new Recommendation
            {
                AnalysisId = analysis.Id,
                Description = "Completed recommendation",
                Status = RecommendationStatus.Completed
            };

            await _repository.AddAsync(pendingRecommendation);
            await _repository.AddAsync(completedRecommendation);
            await _repository.SaveChangesAsync();

            // Act
            var pendingRecommendations = await _repository.GetRecommendationsByStatusAsync(RecommendationStatus.Pending);

            // Assert
            Assert.AreEqual(1, pendingRecommendations.Count());
            Assert.AreEqual("Pending recommendation", pendingRecommendations.First().Description);
        }

        [TestMethod]
        public async Task GetRecommendationsByPriorityAsync_WithHighPriority_ShouldReturnHighPriorityRecommendations()
        {
            // Arrange
            var report = new SafetyReport { FileName = "test_report.pdf", UploadedBy = "test@example.com" };
            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            var analysis = new AnalysisResult { ReportId = report.Id, IncidentType = "Slip", Severity = "Medium", RiskScore = 5 };
            await _analysisRepository.AddAsync(analysis);
            await _analysisRepository.SaveChangesAsync();

            var highPriorityRecommendation = new Recommendation
            {
                AnalysisId = analysis.Id,
                Description = "High priority recommendation",
                Priority = "High"
            };

            var lowPriorityRecommendation = new Recommendation
            {
                AnalysisId = analysis.Id,
                Description = "Low priority recommendation",
                Priority = "Low"
            };

            await _repository.AddAsync(highPriorityRecommendation);
            await _repository.AddAsync(lowPriorityRecommendation);
            await _repository.SaveChangesAsync();

            // Act
            var highPriorityRecommendations = await _repository.GetRecommendationsByPriorityAsync("High");

            // Assert
            Assert.AreEqual(1, highPriorityRecommendations.Count());
            Assert.AreEqual("High priority recommendation", highPriorityRecommendations.First().Description);
        }

        [TestMethod]
        public async Task GetPendingRecommendationsAsync_ShouldReturnPendingRecommendationsOrderedByPriority()
        {
            // Arrange
            var report = new SafetyReport { FileName = "test_report.pdf", UploadedBy = "test@example.com" };
            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            var analysis = new AnalysisResult { ReportId = report.Id, IncidentType = "Slip", Severity = "Medium", RiskScore = 5 };
            await _analysisRepository.AddAsync(analysis);
            await _analysisRepository.SaveChangesAsync();

            var criticalRecommendation = new Recommendation
            {
                AnalysisId = analysis.Id,
                Description = "Critical recommendation",
                Priority = "Critical",
                Status = RecommendationStatus.Pending
            };

            var mediumRecommendation = new Recommendation
            {
                AnalysisId = analysis.Id,
                Description = "Medium recommendation",
                Priority = "Medium",
                Status = RecommendationStatus.Pending
            };

            var completedRecommendation = new Recommendation
            {
                AnalysisId = analysis.Id,
                Description = "Completed recommendation",
                Priority = "High",
                Status = RecommendationStatus.Completed
            };

            await _repository.AddAsync(criticalRecommendation);
            await _repository.AddAsync(mediumRecommendation);
            await _repository.AddAsync(completedRecommendation);
            await _repository.SaveChangesAsync();

            // Act
            var pendingRecommendations = await _repository.GetPendingRecommendationsAsync();

            // Assert
            Assert.AreEqual(2, pendingRecommendations.Count());
            Assert.AreEqual("Critical recommendation", pendingRecommendations.First().Description);
        }
    }
}