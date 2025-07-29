using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SafetyAI.Models.Enums;

namespace SafetyAI.Models.Entities
{
    public class Recommendation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AnalysisId { get; set; }

        [StringLength(100)]
        public string RecommendationType { get; set; }

        [Required]
        public string Description { get; set; }

        [StringLength(50)]
        public string Priority { get; set; }

        public decimal? EstimatedCost { get; set; }

        public int? EstimatedTimeHours { get; set; }

        [StringLength(100)]
        public string ResponsibleRole { get; set; }

        public RecommendationStatus Status { get; set; }

        // Navigation Properties
        [ForeignKey("AnalysisId")]
        public virtual AnalysisResult AnalysisResult { get; set; }

        public Recommendation()
        {
            Id = Guid.NewGuid();
            Status = RecommendationStatus.Pending;
        }
    }
}