using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using SafetyAI.Data.Context;
using SafetyAI.Data.Interfaces;
using SafetyAI.Models.Entities;

namespace SafetyAI.Data.Repositories
{
    public class AnalysisResultRepository : BaseRepository<AnalysisResult>, IAnalysisResultRepository
    {
        public AnalysisResultRepository(SafetyAIDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<AnalysisResult>> GetAnalysisByIncidentTypeAsync(string incidentType)
        {
            return await _dbSet
                .Where(ar => ar.IncidentType == incidentType)
                .Include(ar => ar.SafetyReport)
                .OrderByDescending(ar => ar.AnalysisDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AnalysisResult>> GetAnalysisBySeverityAsync(string severity)
        {
            return await _dbSet
                .Where(ar => ar.Severity == severity)
                .Include(ar => ar.SafetyReport)
                .OrderByDescending(ar => ar.AnalysisDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AnalysisResult>> GetAnalysisWithLowConfidenceAsync(decimal threshold)
        {
            return await _dbSet
                .Where(ar => ar.ConfidenceLevel < threshold)
                .Include(ar => ar.SafetyReport)
                .Include(ar => ar.Recommendations)
                .OrderByDescending(ar => ar.AnalysisDate)
                .ToListAsync();
        }

        public async Task<AnalysisResult> GetAnalysisWithRecommendationsAsync(Guid analysisId)
        {
            return await _dbSet
                .Include(ar => ar.SafetyReport)
                .Include(ar => ar.Recommendations)
                .FirstOrDefaultAsync(ar => ar.Id == analysisId);
        }

        public async Task<IEnumerable<AnalysisResult>> GetAnalysisByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(ar => ar.AnalysisDate >= startDate && ar.AnalysisDate <= endDate)
                .Include(ar => ar.SafetyReport)
                .OrderByDescending(ar => ar.AnalysisDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AnalysisResult>> GetCriticalIncidentsAsync()
        {
            return await _dbSet
                .Where(ar => ar.Severity == "Critical" || ar.RiskScore >= 8)
                .Include(ar => ar.SafetyReport)
                .Include(ar => ar.Recommendations)
                .OrderByDescending(ar => ar.AnalysisDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetIncidentTypeStatisticsAsync()
        {
            return await _dbSet
                .GroupBy(ar => ar.IncidentType)
                .Select(g => new { IncidentType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.IncidentType, x => x.Count);
        }

        public async Task<Dictionary<string, int>> GetSeverityStatisticsAsync()
        {
            return await _dbSet
                .GroupBy(ar => ar.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Severity, x => x.Count);
        }

        public async Task<double> GetAverageProcessingTimeAsync()
        {
            return await _dbSet
                .Where(ar => ar.ProcessingTimeMs > 0)
                .AverageAsync(ar => ar.ProcessingTimeMs);
        }

        public async Task<double> GetAverageConfidenceLevelAsync()
        {
            return await _dbSet
                .AverageAsync(ar => (double)ar.ConfidenceLevel);
        }
    }
}