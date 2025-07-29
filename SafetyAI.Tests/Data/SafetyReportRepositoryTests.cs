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
    public class SafetyReportRepositoryTests
    {
        private TestDbContext _context;
        private SafetyReportRepository _repository;

        [TestInitialize]
        public void Setup()
        {
            _context = new TestDbContext();
            _repository = new SafetyReportRepository(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
            _repository?.Dispose();
        }

        [TestMethod]
        public async Task AddAsync_ValidReport_ShouldAddToDatabase()
        {
            // Arrange
            var report = new SafetyReport
            {
                FileName = "test_report.pdf",
                FileSize = 1024,
                FileType = "application/pdf",
                ExtractedText = "Test incident report",
                UploadedBy = "test@example.com"
            };

            // Act
            await _repository.AddAsync(report);
            await _repository.SaveChangesAsync();

            // Assert
            var savedReport = await _repository.GetByIdAsync(report.Id);
            Assert.IsNotNull(savedReport);
            Assert.AreEqual("test_report.pdf", savedReport.FileName);
            Assert.AreEqual(ProcessingStatus.Pending, savedReport.Status);
            Assert.IsTrue(savedReport.IsActive);
        }

        [TestMethod]
        public async Task GetReportsByStatusAsync_WithPendingStatus_ShouldReturnPendingReports()
        {
            // Arrange
            var pendingReport = new SafetyReport
            {
                FileName = "pending_report.pdf",
                Status = ProcessingStatus.Pending,
                UploadedBy = "test@example.com"
            };

            var completedReport = new SafetyReport
            {
                FileName = "completed_report.pdf",
                Status = ProcessingStatus.Completed,
                UploadedBy = "test@example.com"
            };

            await _repository.AddAsync(pendingReport);
            await _repository.AddAsync(completedReport);
            await _repository.SaveChangesAsync();

            // Act
            var pendingReports = await _repository.GetReportsByStatusAsync(ProcessingStatus.Pending);

            // Assert
            Assert.AreEqual(1, pendingReports.Count());
            Assert.AreEqual("pending_report.pdf", pendingReports.First().FileName);
        }

        [TestMethod]
        public async Task GetReportsByUserAsync_WithValidUser_ShouldReturnUserReports()
        {
            // Arrange
            var user1Report = new SafetyReport
            {
                FileName = "user1_report.pdf",
                UploadedBy = "user1@example.com"
            };

            var user2Report = new SafetyReport
            {
                FileName = "user2_report.pdf",
                UploadedBy = "user2@example.com"
            };

            await _repository.AddAsync(user1Report);
            await _repository.AddAsync(user2Report);
            await _repository.SaveChangesAsync();

            // Act
            var user1Reports = await _repository.GetReportsByUserAsync("user1@example.com");

            // Assert
            Assert.AreEqual(1, user1Reports.Count());
            Assert.AreEqual("user1_report.pdf", user1Reports.First().FileName);
        }

        [TestMethod]
        public async Task SearchReportsAsync_WithSearchTerm_ShouldReturnMatchingReports()
        {
            // Arrange
            var report1 = new SafetyReport
            {
                FileName = "slip_incident.pdf",
                ExtractedText = "Employee slipped on wet floor",
                UploadedBy = "test@example.com"
            };

            var report2 = new SafetyReport
            {
                FileName = "fire_incident.pdf",
                ExtractedText = "Fire alarm activated in building",
                UploadedBy = "test@example.com"
            };

            await _repository.AddAsync(report1);
            await _repository.AddAsync(report2);
            await _repository.SaveChangesAsync();

            // Act
            var searchResults = await _repository.SearchReportsAsync("slip");

            // Assert
            Assert.AreEqual(1, searchResults.Count());
            Assert.AreEqual("slip_incident.pdf", searchResults.First().FileName);
        }

        [TestMethod]
        public async Task GetReportsByDateRangeAsync_WithValidRange_ShouldReturnReportsInRange()
        {
            // Arrange
            var oldReport = new SafetyReport
            {
                FileName = "old_report.pdf",
                UploadedDate = DateTime.UtcNow.AddDays(-10),
                UploadedBy = "test@example.com"
            };

            var recentReport = new SafetyReport
            {
                FileName = "recent_report.pdf",
                UploadedDate = DateTime.UtcNow.AddDays(-1),
                UploadedBy = "test@example.com"
            };

            await _repository.AddAsync(oldReport);
            await _repository.AddAsync(recentReport);
            await _repository.SaveChangesAsync();

            // Act
            var startDate = DateTime.UtcNow.AddDays(-5);
            var endDate = DateTime.UtcNow;
            var reportsInRange = await _repository.GetReportsByDateRangeAsync(startDate, endDate);

            // Assert
            Assert.AreEqual(1, reportsInRange.Count());
            Assert.AreEqual("recent_report.pdf", reportsInRange.First().FileName);
        }
    }
}