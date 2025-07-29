using System.Collections.Generic;

namespace SafetyAI.Models.DTOs
{
    public class AudioProcessingResult
    {
        public string TranscribedText { get; set; }
        public string DetectedLanguage { get; set; }
        public double TranscriptionConfidence { get; set; }
        public List<string> SafetyTermsIdentified { get; set; }
        public bool RequiresReprocessing { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccess { get; set; }

        public AudioProcessingResult()
        {
            SafetyTermsIdentified = new List<string>();
            IsSuccess = true;
        }
    }
}