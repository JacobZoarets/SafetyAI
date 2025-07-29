using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SafetyAI.Models.Entities;
using SafetyAI.Models.Enums;

namespace SafetyAI.Data.Interfaces
{
    public interface IRecommendationRepository : IRepository<Recommendation>
    {
        Task<IEnumerable<Recommendation>> GetRecommendationsByStatusAsync(RecommendationStatus status);
        Task<IEnumerable<Recommendation>> GetRecommendationsByPriorityAsync(string priority);
        Task<IEnumerable<Recommendation>> GetRecommendationsByResponsibleRoleAsync(string role);
        Task<IEnumerable<Recommendation>> GetPendingRecommendationsAsync();
        Task<IEnumerable<Recommendation>> GetRecommendationsByAnalysisIdAsync(Guid analysisId);
    }
}