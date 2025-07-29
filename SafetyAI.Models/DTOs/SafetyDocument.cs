using System;

namespace SafetyAI.Models.DTOs
{
    public class SafetyDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string DocumentType { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Source { get; set; }
        public double RelevanceScore { get; set; }
    }
}