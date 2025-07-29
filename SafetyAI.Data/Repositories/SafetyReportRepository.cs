using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using SafetyAI.Data.Context;
using SafetyAI.Data.Interfaces;
using SafetyAI.Models.Entities;
using SafetyAI.Models.Enums;

namespace SafetyAI.Data.Repositories
{
    public class SafetyReportRepository : BaseRepository<SafetyReport>, ISafetyReportRepository
    {
        public SafetyReportRepository(SafetyAIDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SafetyReport>> GetReportsByStatusAsync(ProcessingStatus status)
        {
            return await _dbSet
                .Where(sr => sr.Status == status && sr.IsActive)
                .OrderByDescending(sr => sr.UploadedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SafetyReport>> GetReportsByUserAsync(string userId)
        {
            return await _dbSet
                .Where(sr => sr.UploadedBy == userId && sr.IsActive)
                .OrderByDescending(sr => sr.UploadedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SafetyReport>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(sr => sr.UploadedDate >= startDate && sr.UploadedDate <= endDate && sr.IsActive)
                .OrderByDescending(sr => sr.UploadedDate)
                .ToListAsync();
        }

        public async Task<SafetyReport> GetReportWithAnalysisAsync(Guid reportId)
        {
            return await _dbSet
                .Include(sr => sr.AnalysisResults)
                .Include(sr => sr.AnalysisResults.Select(ar => ar.Recommendations))
                .FirstOrDefaultAsync(sr => sr.Id == reportId && sr.IsActive);
        }

        public async Task<IEnumerable<SafetyReport>> SearchReportsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            var lowerSearchTerm = searchTerm.ToLower();
            
            return await _dbSet
                .Where(sr => sr.IsActive && (
                    sr.FileName.ToLower().Contains(lowerSearchTerm) ||
                    sr.ExtractedText.ToLower().Contains(lowerSearchTerm) ||
                    sr.UploadedBy.ToLower().Contains(lowerSearchTerm)
                ))
                .OrderByDescending(sr => sr.UploadedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SafetyReport>> GetRecentReportsAsync(int count = 10)
        {
            return await _dbSet
                .Where(sr => sr.IsActive)
                .OrderByDescending(sr => sr.UploadedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<SafetyReport>> GetAllReportsAsync()
        {
            return await _dbSet
                .Where(sr => sr.IsActive)
                .OrderByDescending(sr => sr.UploadedDate)
                .ToListAsync();
        }

        public async Task<int> GetReportCountByStatusAsync(ProcessingStatus status)
        {
            return await _dbSet
                .CountAsync(sr => sr.Status == status && sr.IsActive);
        }

        public async Task<IEnumerable<SafetyReport>> GetPendingReportsAsync()
        {
            return await _dbSet
                .Where(sr => sr.Status == ProcessingStatus.Pending && sr.IsActive)
                .OrderBy(sr => sr.UploadedDate)
                .ToListAsync();
        }
    }
}