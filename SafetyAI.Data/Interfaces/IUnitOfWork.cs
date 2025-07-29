using System;
using System.Threading.Tasks;

namespace SafetyAI.Data.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ISafetyReportRepository SafetyReports { get; }
        IAnalysisResultRepository AnalysisResults { get; }
        IRecommendationRepository Recommendations { get; }
        
        Task<int> SaveChangesAsync();
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }
}