using System;
using System.Collections.Generic;

namespace SafetyAI.Models.DTOs
{
    public class DocumentAnalysisResult
    {
        public string ExtractedText { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> DetectedLanguages { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public bool RequiresHumanReview { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccess { get; set; }

        public DocumentAnalysisResult()
        {
            DetectedLanguages = new List<string>();
            IsSuccess = true;
        }
    }
}