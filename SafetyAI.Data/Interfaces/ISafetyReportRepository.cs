using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SafetyAI.Models.Entities;
using SafetyAI.Models.Enums;

namespace SafetyAI.Data.Interfaces
{
    public interface ISafetyReportRepository : IRepository<SafetyReport>
    {
        Task<IEnumerable<SafetyReport>> GetReportsByStatusAsync(ProcessingStatus status);
        Task<IEnumerable<SafetyReport>> GetReportsByUserAsync(string userId);
        Task<IEnumerable<SafetyReport>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<SafetyReport> GetReportWithAnalysisAsync(Guid reportId);
        Task<IEnumerable<SafetyReport>> SearchReportsAsync(string searchTerm);
        Task<IEnumerable<SafetyReport>> GetRecentReportsAsync(int count = 10);
        Task<IEnumerable<SafetyReport>> GetAllReportsAsync();
        Task<int> GetReportCountByStatusAsync(ProcessingStatus status);
    }
}