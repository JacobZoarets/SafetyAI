using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Data.Repositories;
using SafetyAI.Models.Entities;
using SafetyAI.Tests.Helpers;

namespace SafetyAI.Tests.Data
{
    [TestClass]
    public class AnalysisResultRepositoryTests
    {
        private TestDbContext _context;
        private AnalysisResultRepository _repository;
        private SafetyReportRepository _reportRepository;

        [TestInitialize]
        public void Setup()
        {
            _context = new TestDbContext();
            _repository = new AnalysisResultRepository(_context);
            _reportRepository = new SafetyReportRepository(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
            _repository?.Dispose();
            _reportRepository?.Dispose();
        }

        [TestMethod]
        public async Task AddAsync_ValidAnalysisResult_ShouldAddToDatabase()
        {
            // Arrange
            var report = new SafetyReport
            {
                FileName = "test_report.pdf",
                UploadedBy = "test@example.com"
            };

            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            var analysisResult = new AnalysisResult
            {
                ReportId = report.Id,
                IncidentType = "Slip",
                Severity = "Medium",
                RiskScore = 5,
                Summary = "Employee slipped on wet floor",
                ConfidenceLevel = 0.92m,
                ProcessingTimeMs = 15000
            };

            // Act
            await _repository.AddAsync(analysisResult);
            await _repository.SaveChangesAsync();

            // Assert
            var savedResult = await _repository.GetByIdAsync(analysisResult.Id);
            Assert.IsNotNull(savedResult);
            Assert.AreEqual("Slip", savedResult.IncidentType);
            Assert.AreEqual("Medium", savedResult.Severity);
            Assert.AreEqual(5, savedResult.RiskScore);
        }

        [TestMethod]
        public async Task GetAnalysisByIncidentTypeAsync_WithValidType_ShouldReturnMatchingResults()
        {
            // Arrange
            var report1 = new SafetyReport { FileName = "report1.pdf", UploadedBy = "test@example.com" };
            var report2 = new SafetyReport { FileName = "report2.pdf", UploadedBy = "test@example.com" };

            await _reportRepository.AddAsync(report1);
            await _reportRepository.AddAsync(report2);
            await _reportRepository.SaveChangesAsync();

            var slipAnalysis = new AnalysisResult
            {
                ReportId = report1.Id,
                IncidentType = "Slip",
                Severity = "Medium",
                RiskScore = 5
            };

            var fallAnalysis = new AnalysisResult
            {
                ReportId = report2.Id,
                IncidentType = "Fall",
                Severity = "High",
                RiskScore = 8
            };

            await _repository.AddAsync(slipAnalysis);
            await _repository.AddAsync(fallAnalysis);
            await _repository.SaveChangesAsync();

            // Act
            var slipResults = await _repository.GetAnalysisByIncidentTypeAsync("Slip");

            // Assert
            Assert.AreEqual(1, slipResults.Count());
            Assert.AreEqual("Slip", slipResults.First().IncidentType);
        }

        [TestMethod]
        public async Task GetAnalysisBySeverityAsync_WithValidSeverity_ShouldReturnMatchingResults()
        {
            // Arrange
            var report1 = new SafetyReport { FileName = "report1.pdf", UploadedBy = "test@example.com" };
            var report2 = new SafetyReport { FileName = "report2.pdf", UploadedBy = "test@example.com" };

            await _reportRepository.AddAsync(report1);
            await _reportRepository.AddAsync(report2);
            await _reportRepository.SaveChangesAsync();

            var criticalAnalysis = new AnalysisResult
            {
                ReportId = report1.Id,
                IncidentType = "Fall",
                Severity = "Critical",
                RiskScore = 9
            };

            var mediumAnalysis = new AnalysisResult
            {
                ReportId = report2.Id,
                IncidentType = "Slip",
                Severity = "Medium",
                RiskScore = 5
            };

            await _repository.AddAsync(criticalAnalysis);
            await _repository.AddAsync(mediumAnalysis);
            await _repository.SaveChangesAsync();

            // Act
            var criticalResults = await _repository.GetAnalysisBySeverityAsync("Critical");

            // Assert
            Assert.AreEqual(1, criticalResults.Count());
            Assert.AreEqual("Critical", criticalResults.First().Severity);
        }

        [TestMethod]
        public async Task GetAnalysisWithLowConfidenceAsync_WithThreshold_ShouldReturnLowConfidenceResults()
        {
            // Arrange
            var report1 = new SafetyReport { FileName = "report1.pdf", UploadedBy = "test@example.com" };
            var report2 = new SafetyReport { FileName = "report2.pdf", UploadedBy = "test@example.com" };

            await _reportRepository.AddAsync(report1);
            await _reportRepository.AddAsync(report2);
            await _reportRepository.SaveChangesAsync();

            var lowConfidenceAnalysis = new AnalysisResult
            {
                ReportId = report1.Id,
                IncidentType = "Other",
                Severity = "Low",
                RiskScore = 3,
                ConfidenceLevel = 0.65m
            };

            var highConfidenceAnalysis = new AnalysisResult
            {
                ReportId = report2.Id,
                IncidentType = "Slip",
                Severity = "Medium",
                RiskScore = 5,
                ConfidenceLevel = 0.95m
            };

            await _repository.AddAsync(lowConfidenceAnalysis);
            await _repository.AddAsync(highConfidenceAnalysis);
            await _repository.SaveChangesAsync();

            // Act
            var lowConfidenceResults = await _repository.GetAnalysisWithLowConfidenceAsync(0.8m);

            // Assert
            Assert.AreEqual(1, lowConfidenceResults.Count());
            Assert.AreEqual(0.65m, lowConfidenceResults.First().ConfidenceLevel);
        }
    }
}