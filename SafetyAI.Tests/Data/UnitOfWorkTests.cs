using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Data.Repositories;
using SafetyAI.Models.Entities;
using SafetyAI.Tests.Helpers;

namespace SafetyAI.Tests.Data
{
    [TestClass]
    public class UnitOfWorkTests
    {
        private TestDbContext _context;
        private UnitOfWork _unitOfWork;

        [TestInitialize]
        public void Setup()
        {
            _context = new TestDbContext();
            _unitOfWork = new UnitOfWork(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _unitOfWork?.Dispose();
            _context?.Dispose();
        }

        [TestMethod]
        public void Constructor_WithValidContext_ShouldInitializeRepositories()
        {
            // Assert
            Assert.IsNotNull(_unitOfWork.SafetyReports);
            Assert.IsNotNull(_unitOfWork.AnalysisResults);
            Assert.IsNotNull(_unitOfWork.Recommendations);
        }

        [TestMethod]
        public async Task SaveChangesAsync_WithValidChanges_ShouldPersistToDatabase()
        {
            // Arrange
            var report = new SafetyReport
            {
                FileName = "test_report.pdf",
                UploadedBy = "test@example.com"
            };

            // Act
            await _unitOfWork.SafetyReports.AddAsync(report);
            var result = await _unitOfWork.SaveChangesAsync();

            // Assert
            Assert.AreEqual(1, result);
            
            var savedReport = await _unitOfWork.SafetyReports.GetByIdAsync(report.Id);
            Assert.IsNotNull(savedReport);
            Assert.AreEqual("test_report.pdf", savedReport.FileName);
        }

        [TestMethod]
        public async Task Transaction_WithCommit_ShouldPersistChanges()
        {
            // Arrange
            var report = new SafetyReport
            {
                FileName = "transaction_test.pdf",
                UploadedBy = "test@example.com"
            };

            // Act
            _unitOfWork.BeginTransaction();
            
            await _unitOfWork.SafetyReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();
            
            _unitOfWork.CommitTransaction();

            // Assert
            var savedReport = await _unitOfWork.SafetyReports.GetByIdAsync(report.Id);
            Assert.IsNotNull(savedReport);
        }

        [TestMethod]
        public async Task Transaction_WithRollback_ShouldNotPersistChanges()
        {
            // Arrange
            var report = new SafetyReport
            {
                FileName = "rollback_test.pdf",
                UploadedBy = "test@example.com"
            };

            // Act
            _unitOfWork.BeginTransaction();
            
            await _unitOfWork.SafetyReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();
            
            _unitOfWork.RollbackTransaction();

            // Assert
            var savedReport = await _unitOfWork.SafetyReports.GetByIdAsync(report.Id);
            Assert.IsNull(savedReport);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void BeginTransaction_WhenTransactionAlreadyStarted_ShouldThrowException()
        {
            // Act
            _unitOfWork.BeginTransaction();
            _unitOfWork.BeginTransaction(); // Should throw exception
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CommitTransaction_WhenNoTransactionStarted_ShouldThrowException()
        {
            // Act
            _unitOfWork.CommitTransaction(); // Should throw exception
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void RollbackTransaction_WhenNoTransactionStarted_ShouldThrowException()
        {
            // Act
            _unitOfWork.RollbackTransaction(); // Should throw exception
        }
    }
}