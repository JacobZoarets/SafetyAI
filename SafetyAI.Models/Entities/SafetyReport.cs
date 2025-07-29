using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SafetyAI.Models.Enums;

namespace SafetyAI.Models.Entities
{
    public class SafetyReport
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        public int FileSize { get; set; }

        [StringLength(50)]
        public string FileType { get; set; }

        public string OriginalText { get; set; }

        public string ExtractedText { get; set; }

        public ProcessingStatus Status { get; set; }

        [StringLength(100)]
        public string UploadedBy { get; set; }

        public DateTime UploadedDate { get; set; }

        public DateTime? ProcessedDate { get; set; }

        public bool IsActive { get; set; }

        // Navigation Properties
        public virtual ICollection<AnalysisResult> AnalysisResults { get; set; }
        public virtual ICollection<Recommendation> Recommendations { get; set; }

        public SafetyReport()
        {
            Id = Guid.NewGuid();
            UploadedDate = DateTime.UtcNow;
            Status = ProcessingStatus.Pending;
            IsActive = true;
            AnalysisResults = new HashSet<AnalysisResult>();
            Recommendations = new HashSet<Recommendation>();
        }
    }
}