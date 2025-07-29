using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SafetyAI.Models.Entities;

namespace SafetyAI.Data.Interfaces
{
    public interface IAnalysisResultRepository : IRepository<AnalysisResult>
    {
        Task<IEnumerable<AnalysisResult>> GetAnalysisByIncidentTypeAsync(string incidentType);
        Task<IEnumerable<AnalysisResult>> GetAnalysisBySeverityAsync(string severity);
        Task<IEnumerable<AnalysisResult>> GetAnalysisWithLowConfidenceAsync(decimal threshold);
        Task<AnalysisResult> GetAnalysisWithRecommendationsAsync(Guid analysisId);
        Task<IEnumerable<AnalysisResult>> GetAnalysisByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<AnalysisResult>> GetCriticalIncidentsAsync();
    }
}