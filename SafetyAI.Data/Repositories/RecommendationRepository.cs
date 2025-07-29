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
    public class RecommendationRepository : BaseRepository<Recommendation>, IRecommendationRepository
    {
        public RecommendationRepository(SafetyAIDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Recommendation>> GetRecommendationsByStatusAsync(RecommendationStatus status)
        {
            return await _dbSet
                .Where(r => r.Status == status)
                .Include(r => r.AnalysisResult)
                .Include(r => r.AnalysisResult.SafetyReport)
                .OrderByDescending(r => r.AnalysisResult.AnalysisDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Recommendation>> GetRecommendationsByPriorityAsync(string priority)
        {
            return await _dbSet
                .Where(r => r.Priority == priority)
                .Include(r => r.AnalysisResult)
                .Include(r => r.AnalysisResult.SafetyReport)
                .OrderByDescending(r => r.AnalysisResult.AnalysisDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Recommendation>> GetRecommendationsByResponsibleRoleAsync(string role)
        {
            return await _dbSet
                .Where(r => r.ResponsibleRole == role)
                .Include(r => r.AnalysisResult)
                .Include(r => r.AnalysisResult.SafetyReport)
                .OrderByDescending(r => r.AnalysisResult.AnalysisDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Recommendation>> GetPendingRecommendationsAsync()
        {
            return await _dbSet
                .Where(r => r.Status == RecommendationStatus.Pending)
                .Include(r => r.AnalysisResult)
                .Include(r => r.AnalysisResult.SafetyReport)
                .OrderBy(r => r.Priority == "Critical" ? 1 : r.Priority == "High" ? 2 : r.Priority == "Medium" ? 3 : 4)
                .ThenByDescending(r => r.AnalysisResult.AnalysisDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Recommendation>> GetRecommendationsByAnalysisIdAsync(Guid analysisId)
        {
            return await _dbSet
                .Where(r => r.AnalysisId == analysisId)
                .OrderBy(r => r.Priority == "Critical" ? 1 : r.Priority == "High" ? 2 : r.Priority == "Medium" ? 3 : 4)
                .ToListAsync();
        }

        public async Task<IEnumerable<Recommendation>> GetHighPriorityRecommendationsAsync()
        {
            return await _dbSet
                .Where(r => (r.Priority == "Critical" || r.Priority == "High") && r.Status == RecommendationStatus.Pending)
                .Include(r => r.AnalysisResult)
                .Include(r => r.AnalysisResult.SafetyReport)
                .OrderBy(r => r.Priority == "Critical" ? 1 : 2)
                .ThenByDescending(r => r.AnalysisResult.AnalysisDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetRecommendationStatusStatisticsAsync()
        {
            return await _dbSet
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<Dictionary<string, int>> GetRecommendationPriorityStatisticsAsync()
        {
            return await _dbSet
                .GroupBy(r => r.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Priority, x => x.Count);
        }

        public async Task<decimal> GetTotalEstimatedCostAsync()
        {
            return await _dbSet
                .Where(r => r.EstimatedCost.HasValue)
                .SumAsync(r => r.EstimatedCost.Value);
        }

        public async Task<int> GetTotalEstimatedTimeAsync()
        {
            return await _dbSet
                .Where(r => r.EstimatedTimeHours.HasValue)
                .SumAsync(r => r.EstimatedTimeHours.Value);
        }
    }
}