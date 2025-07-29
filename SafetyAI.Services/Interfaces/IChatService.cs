using System.Collections.Generic;
using System.Threading.Tasks;
using SafetyAI.Models.DTOs;

namespace SafetyAI.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponse> ProcessQueryAsync(string query, string sessionId, SafetyAI.Models.DTOs.UserContext context);
        Task<List<SafetyDocument>> RetrieveRelevantDocumentsAsync(string query);
        Task<bool> RequiresEscalationAsync(string query, ChatResponse response);
    }
}