using System.Data.Entity;
using SafetyAI.Models.Entities;
using SafetyAI.Data.Migrations;

namespace SafetyAI.Data.Context
{
    public class SafetyAIDbContext : DbContext
    {
        public SafetyAIDbContext() : base("SafetyAIConnection")
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<SafetyAIDbContext, Configuration>());
        }

        public DbSet<SafetyReport> SafetyReports { get; set; }
        public DbSet<AnalysisResult> AnalysisResults { get; set; }
        public DbSet<Recommendation> Recommendations { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configure SafetyReport entity
            modelBuilder.Entity<SafetyReport>()
                .HasKey(sr => sr.Id)
                .Property(sr => sr.FileName)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<SafetyReport>()
                .Property(sr => sr.FileType)
                .HasMaxLength(50);

            modelBuilder.Entity<SafetyReport>()
                .Property(sr => sr.UploadedBy)
                .HasMaxLength(100);

            // Create indexes for SafetyReport
            modelBuilder.Entity<SafetyReport>()
                .HasIndex(sr => sr.Status)
                .HasName("IX_SafetyReport_Status");

            modelBuilder.Entity<SafetyReport>()
                .HasIndex(sr => sr.UploadedDate)
                .HasName("IX_SafetyReport_UploadedDate");

            modelBuilder.Entity<SafetyReport>()
                .HasIndex(sr => sr.UploadedBy)
                .HasName("IX_SafetyReport_UploadedBy");

            modelBuilder.Entity<SafetyReport>()
                .HasIndex(sr => sr.IsActive)
                .HasName("IX_SafetyReport_IsActive");

            // Configure AnalysisResult entity
            modelBuilder.Entity<AnalysisResult>()
                .HasKey(ar => ar.Id);
                
            // Configure relationship: SafetyReport (principal) -> AnalysisResult (dependent)
            // Note: This is actually a one-to-many relationship due to foreign key structure
            modelBuilder.Entity<SafetyReport>()
                .HasMany(sr => sr.AnalysisResults)
                .WithRequired(ar => ar.SafetyReport)
                .HasForeignKey(ar => ar.ReportId);

            modelBuilder.Entity<AnalysisResult>()
                .Property(ar => ar.IncidentType)
                .HasMaxLength(100);

            modelBuilder.Entity<AnalysisResult>()
                .Property(ar => ar.Severity)
                .HasMaxLength(50);

            modelBuilder.Entity<AnalysisResult>()
                .Property(ar => ar.Summary)
                .HasMaxLength(500);

            modelBuilder.Entity<AnalysisResult>()
                .Property(ar => ar.AIModel)
                .HasMaxLength(100);

            // ConfidenceLevel will use EF default decimal precision

            // Create indexes for AnalysisResult
            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(ar => ar.IncidentType)
                .HasName("IX_AnalysisResult_IncidentType");

            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(ar => ar.Severity)
                .HasName("IX_AnalysisResult_Severity");

            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(ar => ar.AnalysisDate)
                .HasName("IX_AnalysisResult_AnalysisDate");

            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(ar => ar.RiskScore)
                .HasName("IX_AnalysisResult_RiskScore");

            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(ar => ar.ConfidenceLevel)
                .HasName("IX_AnalysisResult_ConfidenceLevel");

            // Configure Recommendation entity
            modelBuilder.Entity<Recommendation>()
                .HasKey(r => r.Id);
                
            // Configure relationship: AnalysisResult (principal) -> Recommendation (dependent)
            modelBuilder.Entity<AnalysisResult>()
                .HasMany(ar => ar.Recommendations)
                .WithRequired(r => r.AnalysisResult);

            modelBuilder.Entity<Recommendation>()
                .Property(r => r.RecommendationType)
                .HasMaxLength(100);

            modelBuilder.Entity<Recommendation>()
                .Property(r => r.Description)
                .IsRequired();

            modelBuilder.Entity<Recommendation>()
                .Property(r => r.Priority)
                .HasMaxLength(50);

            modelBuilder.Entity<Recommendation>()
                .Property(r => r.ResponsibleRole)
                .HasMaxLength(100);

            // EstimatedCost will use EF default decimal precision

            // Create indexes for Recommendation
            modelBuilder.Entity<Recommendation>()
                .HasIndex(r => r.Status)
                .HasName("IX_Recommendation_Status");

            modelBuilder.Entity<Recommendation>()
                .HasIndex(r => r.Priority)
                .HasName("IX_Recommendation_Priority");

            modelBuilder.Entity<Recommendation>()
                .HasIndex(r => r.ResponsibleRole)
                .HasName("IX_Recommendation_ResponsibleRole");

            // Composite indexes for common queries
            modelBuilder.Entity<SafetyReport>()
                .HasIndex(sr => new { sr.Status, sr.UploadedDate })
                .HasName("IX_SafetyReport_Status_UploadedDate");

            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(ar => new { ar.Severity, ar.AnalysisDate })
                .HasName("IX_AnalysisResult_Severity_AnalysisDate");

            modelBuilder.Entity<Recommendation>()
                .HasIndex(r => new { r.Status, r.Priority })
                .HasName("IX_Recommendation_Status_Priority");

            base.OnModelCreating(modelBuilder);
        }
    }
}