using System.Collections.Generic;

namespace SafetyAI.Models.DTOs
{
    public class ChatResponse
    {
        public string Response { get; set; }
        public List<SafetyDocument> ReferencedDocuments { get; set; }
        public List<string> SuggestedActions { get; set; }
        public bool RequiresHumanReview { get; set; }
        public double ConfidenceScore { get; set; }
        public string SessionId { get; set; }

        public ChatResponse()
        {
            ReferencedDocuments = new List<SafetyDocument>();
            SuggestedActions = new List<string>();
        }
    }
}