using System.Collections.Generic;

namespace SafetyAI.Models.DTOs
{
    public class VoiceCommandResult
    {
        public string Command { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public bool IsValid { get; set; }
        public double ConfidenceScore { get; set; }

        public VoiceCommandResult()
        {
            Parameters = new Dictionary<string, object>();
            ConfidenceScore = 1.0;
        }
    }
}