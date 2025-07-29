using System;
using System.Data.Entity.Migrations;
using System.Linq;
using SafetyAI.Data.Context;
using SafetyAI.Models.Entities;
using SafetyAI.Models.Enums;

namespace SafetyAI.Data.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<SafetyAIDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
            ContextKey = "SafetyAI.Data.Context.SafetyAIDbContext";
        }

        protected override void Seed(SafetyAIDbContext context)
        {
            // Seed initial data if needed
            SeedInitialData(context);
        }

        public void SeedData(SafetyAIDbContext context)
        {
            SeedInitialData(context);
        }

        private void SeedInitialData(SafetyAIDbContext context)
        {
            // Only seed data if:
            // 1. In debug mode (debugger attached)
            // 2. Database is empty
            // 3. Not explicitly disabled via app setting
            var enableSeeding = System.Configuration.ConfigurationManager.AppSettings["EnableSampleDataSeeding"];
            var seedingEnabled = enableSeeding == null || enableSeeding.ToLower() != "false";
            
            if (System.Diagnostics.Debugger.IsAttached && 
                seedingEnabled && 
                context.SafetyReports.Count() == 0)
            {
                SeedSampleReports(context);
            }
        }

        private void SeedSampleReports(SafetyAIDbContext context)
        {
            // Sample Safety Report 1
            var report1 = new SafetyReport
            {
                Id = Guid.NewGuid(),
                FileName = "sample_incident_001.pdf",
                FileSize = 1024000,
                FileType = "application/pdf",
                ExtractedText = "Employee slipped on wet floor in warehouse area. Minor injury to ankle. First aid administered on site.",
                Status = ProcessingStatus.Completed,
                UploadedBy = "john.doe@company.com",
                UploadedDate = DateTime.UtcNow.AddDays(-5),
                ProcessedDate = DateTime.UtcNow.AddDays(-5).AddMinutes(2),
                IsActive = true
            };

            var analysis1 = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                ReportId = report1.Id,
                IncidentType = "Slip",
                Severity = "Medium",
                RiskScore = 5,
                Summary = "Slip incident due to wet floor conditions. Proper signage and immediate cleanup procedures needed.",
                AnalysisDate = DateTime.UtcNow.AddDays(-5).AddMinutes(2),
                ConfidenceLevel = 0.92m,
                ProcessingTimeMs = 15000,
                AIModel = "Gemini-2.5"
            };

            var recommendation1 = new Recommendation
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysis1.Id,
                RecommendationType = "Preventive",
                Description = "Install additional wet floor warning signs and implement immediate spill cleanup protocol",
                Priority = "High",
                EstimatedCost = 250.00m,
                EstimatedTimeHours = 4,
                ResponsibleRole = "Facility Manager",
                Status = RecommendationStatus.Pending
            };

            // Sample Safety Report 2
            var report2 = new SafetyReport
            {
                Id = Guid.NewGuid(),
                FileName = "equipment_failure_002.jpg",
                FileSize = 2048000,
                FileType = "image/jpeg",
                ExtractedText = "Conveyor belt motor overheated causing production stoppage. No injuries reported. Equipment shut down for inspection.",
                Status = ProcessingStatus.Completed,
                UploadedBy = "jane.smith@company.com",
                UploadedDate = DateTime.UtcNow.AddDays(-3),
                ProcessedDate = DateTime.UtcNow.AddDays(-3).AddMinutes(1),
                IsActive = true
            };

            var analysis2 = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                ReportId = report2.Id,
                IncidentType = "EquipmentFailure",
                Severity = "High",
                RiskScore = 7,
                Summary = "Equipment failure due to motor overheating. Potential fire hazard and production impact.",
                AnalysisDate = DateTime.UtcNow.AddDays(-3).AddMinutes(1),
                ConfidenceLevel = 0.88m,
                ProcessingTimeMs = 12000,
                AIModel = "Gemini-2.5"
            };

            var recommendation2 = new Recommendation
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysis2.Id,
                RecommendationType = "Corrective",
                Description = "Schedule immediate motor inspection and implement preventive maintenance schedule",
                Priority = "Critical",
                EstimatedCost = 1500.00m,
                EstimatedTimeHours = 8,
                ResponsibleRole = "Maintenance Supervisor",
                Status = RecommendationStatus.InProgress
            };

            // Add entities to context
            context.SafetyReports.AddOrUpdate(r => r.Id, report1, report2);
            context.AnalysisResults.AddOrUpdate(a => a.Id, analysis1, analysis2);
            context.Recommendations.AddOrUpdate(r => r.Id, recommendation1, recommendation2);

            // Save changes
            context.SaveChanges();
        }
    }
}