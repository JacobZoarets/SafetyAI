using System;
using System.Data.Entity;
using System.Threading.Tasks;
using SafetyAI.Data.Context;
using SafetyAI.Data.Interfaces;

namespace SafetyAI.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SafetyAIDbContext _context;
        private DbContextTransaction _transaction;
        
        private ISafetyReportRepository _safetyReports;
        private IAnalysisResultRepository _analysisResults;
        private IRecommendationRepository _recommendations;

        public UnitOfWork(SafetyAIDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public ISafetyReportRepository SafetyReports
        {
            get { return _safetyReports ?? (_safetyReports = new SafetyReportRepository(_context)); }
        }

        public IAnalysisResultRepository AnalysisResults
        {
            get { return _analysisResults ?? (_analysisResults = new AnalysisResultRepository(_context)); }
        }

        public IRecommendationRepository Recommendations
        {
            get { return _recommendations ?? (_recommendations = new RecommendationRepository(_context)); }
        }

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error (implement logging in later tasks)
                System.Diagnostics.Debug.WriteLine($"Error saving changes: {ex.Message}");
                throw;
            }
        }

        public void BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }
            
            _transaction = _context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit");
            }

            try
            {
                _transaction.Commit();
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to rollback");
            }

            try
            {
                _transaction.Rollback();
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}