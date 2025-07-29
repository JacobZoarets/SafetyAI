using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SafetyAI.Models.Enums;

namespace SafetyAI.Models.Entities
{
    public class AnalysisResult
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ReportId { get; set; }

        [StringLength(100)]
        public string IncidentType { get; set; }

        [StringLength(50)]
        public string Severity { get; set; }

        [Range(1, 10)]
        public int RiskScore { get; set; }

        [StringLength(500)]
        public string Summary { get; set; }

        public DateTime AnalysisDate { get; set; }

        public decimal ConfidenceLevel { get; set; }

        public int ProcessingTimeMs { get; set; }

        [StringLength(100)]
        public string AIModel { get; set; }

        // Navigation Properties
        [ForeignKey("ReportId")]
        public virtual SafetyReport SafetyReport { get; set; }
        public virtual ICollection<Recommendation> Recommendations { get; set; }

        public AnalysisResult()
        {
            Id = Guid.NewGuid();
            AnalysisDate = DateTime.UtcNow;
            AIModel = "Gemini-2.5";
            Recommendations = new HashSet<Recommendation>();
        }
    }
}