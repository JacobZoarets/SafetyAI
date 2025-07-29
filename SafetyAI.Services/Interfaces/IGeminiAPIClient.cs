using System.Threading.Tasks;
using SafetyAI.Models.DTOs;

namespace SafetyAI.Services.Interfaces
{
    public interface IGeminiAPIClient
    {
        Task<DocumentAnalysisResult> ProcessDocumentAsync(byte[] fileData, string contentType);
        Task<SafetyAnalysisResult> AnalyzeSafetyContentAsync(string text);
        Task<ChatResponse> ProcessChatQueryAsync(string query, string sessionId);
        Task<AudioProcessingResult> ProcessAudioAsync(byte[] audioData, string contentType);
        Task<T> CallWithRetryAsync<T>(System.Func<Task<T>> apiCall);
    }
}