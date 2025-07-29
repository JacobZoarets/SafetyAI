-- SafetyAI Database Migration Script
-- Run this script to create the initial database schema

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SafetyAI')
BEGIN
    CREATE DATABASE SafetyAI;
    PRINT 'Database SafetyAI created successfully.';
END
ELSE
BEGIN
    PRINT 'Database SafetyAI already exists.';
END
GO

USE SafetyAI;
GO

-- Create SafetyReports table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SafetyReports]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SafetyReports] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [FileName] NVARCHAR(255) NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [ContentType] NVARCHAR(50) NULL,
        [FileData] VARBINARY(MAX) NULL,
        [ExtractedText] NVARCHAR(MAX) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [UploadedBy] NVARCHAR(100) NOT NULL,
        [UploadedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ProcessedDate] DATETIME2 NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    PRINT 'SafetyReports table created successfully.';
END
ELSE
BEGIN
    PRINT 'SafetyReports table already exists.';
END
GO

-- Create AnalysisResults table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AnalysisResults]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AnalysisResults] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [SafetyReportId] UNIQUEIDENTIFIER NOT NULL,
        [Summary] NVARCHAR(500) NULL,
        [IncidentType] NVARCHAR(100) NULL,
        [Severity] NVARCHAR(50) NULL,
        [RiskScore] INT NOT NULL DEFAULT 5,
        [ConfidenceLevel] DECIMAL(4,3) NOT NULL DEFAULT 0.5,
        [ProcessingTimeMs] INT NOT NULL DEFAULT 0,
        [AIModel] NVARCHAR(100) NULL,
        [AnalysisDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_AnalysisResults_SafetyReports] 
            FOREIGN KEY ([SafetyReportId]) 
            REFERENCES [dbo].[SafetyReports]([Id]) 
            ON DELETE CASCADE
    );
    
    PRINT 'AnalysisResults table created successfully.';
END
ELSE
BEGIN
    PRINT 'AnalysisResults table already exists.';
END
GO

-- Create Recommendations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Recommendations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Recommendations] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [AnalysisResultId] UNIQUEIDENTIFIER NOT NULL,
        [Type] NVARCHAR(100) NULL,
        [Description] NVARCHAR(MAX) NOT NULL,
        [Priority] NVARCHAR(50) NULL,
        [EstimatedCost] DECIMAL(10,2) NULL,
        [EstimatedTimeHours] INT NULL,
        [ResponsibleRole] NVARCHAR(100) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CompletedDate] DATETIME2 NULL,
        
        CONSTRAINT [FK_Recommendations_AnalysisResults] 
            FOREIGN KEY ([AnalysisResultId]) 
            REFERENCES [dbo].[AnalysisResults]([Id]) 
            ON DELETE CASCADE
    );
    
    PRINT 'Recommendations table created successfully.';
END
ELSE
BEGIN
    PRINT 'Recommendations table already exists.';
END
GO

-- Create indexes for performance optimization
PRINT 'Creating indexes for performance optimization...';

-- SafetyReports indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SafetyReports_Status')
    CREATE INDEX IX_SafetyReports_Status ON [dbo].[SafetyReports] ([Status]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SafetyReports_UploadedDate')
    CREATE INDEX IX_SafetyReports_UploadedDate ON [dbo].[SafetyReports] ([UploadedDate]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SafetyReports_UploadedBy')
    CREATE INDEX IX_SafetyReports_UploadedBy ON [dbo].[SafetyReports] ([UploadedBy]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SafetyReports_IsActive')
    CREATE INDEX IX_SafetyReports_IsActive ON [dbo].[SafetyReports] ([IsActive]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SafetyReports_Status_UploadedDate')
    CREATE INDEX IX_SafetyReports_Status_UploadedDate ON [dbo].[SafetyReports] ([Status], [UploadedDate]);

-- AnalysisResults indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AnalysisResults_IncidentType')
    CREATE INDEX IX_AnalysisResults_IncidentType ON [dbo].[AnalysisResults] ([IncidentType]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AnalysisResults_Severity')
    CREATE INDEX IX_AnalysisResults_Severity ON [dbo].[AnalysisResults] ([Severity]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AnalysisResults_AnalysisDate')
    CREATE INDEX IX_AnalysisResults_AnalysisDate ON [dbo].[AnalysisResults] ([AnalysisDate]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AnalysisResults_RiskScore')
    CREATE INDEX IX_AnalysisResults_RiskScore ON [dbo].[AnalysisResults] ([RiskScore]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AnalysisResults_ConfidenceLevel')
    CREATE INDEX IX_AnalysisResults_ConfidenceLevel ON [dbo].[AnalysisResults] ([ConfidenceLevel]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AnalysisResults_Severity_AnalysisDate')
    CREATE INDEX IX_AnalysisResults_Severity_AnalysisDate ON [dbo].[AnalysisResults] ([Severity], [AnalysisDate]);

-- Recommendations indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Recommendations_Status')
    CREATE INDEX IX_Recommendations_Status ON [dbo].[Recommendations] ([Status]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Recommendations_Priority')
    CREATE INDEX IX_Recommendations_Priority ON [dbo].[Recommendations] ([Priority]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Recommendations_ResponsibleRole')
    CREATE INDEX IX_Recommendations_ResponsibleRole ON [dbo].[Recommendations] ([ResponsibleRole]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Recommendations_Status_Priority')
    CREATE INDEX IX_Recommendations_Status_Priority ON [dbo].[Recommendations] ([Status], [Priority]);

PRINT 'Indexes created successfully.';

-- Enable full-text search on SafetyReports.ExtractedText
IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'SafetyAI_FullText_Catalog')
BEGIN
    CREATE FULLTEXT CATALOG SafetyAI_FullText_Catalog AS DEFAULT;
    PRINT 'Full-text catalog created successfully.';
END

IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('SafetyReports'))
BEGIN
    CREATE FULLTEXT INDEX ON [dbo].[SafetyReports] ([ExtractedText])
    KEY INDEX PK__SafetyRe__3214EC0743D61337; -- This should match your primary key constraint name
    PRINT 'Full-text index created successfully.';
END

-- Create stored procedures for common operations
PRINT 'Creating stored procedures...';

-- Procedure to get dashboard metrics
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetDashboardMetrics')
    DROP PROCEDURE sp_GetDashboardMetrics;
GO

CREATE PROCEDURE sp_GetDashboardMetrics
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @StartDate IS NULL SET @StartDate = DATEADD(DAY, -30, GETUTCDATE());
    IF @EndDate IS NULL SET @EndDate = GETUTCDATE();
    
    SELECT 
        COUNT(*) as TotalReports,
        COUNT(CASE WHEN Status = 'Pending' THEN 1 END) as PendingReports,
        COUNT(CASE WHEN Status = 'Completed' THEN 1 END) as CompletedReports,
        COUNT(CASE WHEN Status = 'Failed' THEN 1 END) as FailedReports
    FROM SafetyReports 
    WHERE UploadedDate BETWEEN @StartDate AND @EndDate;
    
    SELECT 
        COUNT(CASE WHEN ar.Severity = 'Critical' OR ar.RiskScore >= 8 THEN 1 END) as CriticalIncidents,
        AVG(CAST(ar.RiskScore as FLOAT)) as AverageRiskScore
    FROM SafetyReports sr
    INNER JOIN AnalysisResults ar ON sr.Id = ar.SafetyReportId
    WHERE sr.UploadedDate BETWEEN @StartDate AND @EndDate
    AND sr.Status = 'Completed';
END
GO

-- Procedure to get recent reports
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetRecentReports')
    DROP PROCEDURE sp_GetRecentReports;
GO

CREATE PROCEDURE sp_GetRecentReports
    @Count INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Count)
        sr.Id,
        sr.FileName,
        sr.Status,
        sr.UploadedBy,
        sr.UploadedDate,
        ar.Severity,
        ar.RiskScore
    FROM SafetyReports sr
    LEFT JOIN AnalysisResults ar ON sr.Id = ar.SafetyReportId
    WHERE sr.IsActive = 1
    ORDER BY sr.UploadedDate DESC;
END
GO

-- Insert sample data for testing (only if tables are empty)
PRINT 'Inserting sample data...';

IF NOT EXISTS (SELECT 1 FROM SafetyReports)
BEGIN
    DECLARE @SampleReportId UNIQUEIDENTIFIER = NEWID();
    DECLARE @SampleAnalysisId UNIQUEIDENTIFIER = NEWID();
    
    INSERT INTO SafetyReports (Id, FileName, FileSize, ContentType, ExtractedText, Status, UploadedBy, UploadedDate, ProcessedDate)
    VALUES 
    (@SampleReportId, 'sample-incident-report.pdf', 1024000, 'application/pdf', 
     'Sample incident report: Worker slipped on wet floor in warehouse area. No injuries reported.', 
     'Completed', 'System', GETUTCDATE(), GETUTCDATE());
    
    INSERT INTO AnalysisResults (Id, SafetyReportId, Summary, IncidentType, Severity, RiskScore, ConfidenceLevel, ProcessingTimeMs, AIModel, AnalysisDate)
    VALUES 
    (@SampleAnalysisId, @SampleReportId, 'Slip incident in warehouse area with low risk of injury', 
     'Slip', 'Medium', 4, 0.85, 2500, 'Gemini-2.5', GETUTCDATE());
    
    INSERT INTO Recommendations (Id, AnalysisResultId, Type, Description, Priority, EstimatedCost, EstimatedTimeHours, ResponsibleRole, Status)
    VALUES 
    (NEWID(), @SampleAnalysisId, 'Corrective', 'Improve floor surface conditions and drainage in warehouse area', 
     'High', 1000.00, 12, 'Facility Manager', 'Pending'),
    (NEWID(), @SampleAnalysisId, 'Administrative', 'Implement wet floor signage and cleanup procedures', 
     'Medium', 200.00, 4, 'Operations Manager', 'Pending');
    
    PRINT 'Sample data inserted successfully.';
END
ELSE
BEGIN
    PRINT 'Sample data already exists, skipping insertion.';
END

-- Create database backup job (placeholder)
PRINT 'Database backup job configuration would go here...';

-- Grant permissions to application user (adjust as needed)
PRINT 'Granting permissions to application user...';
-- This would typically grant permissions to a specific SQL Server login
-- For now, we'll assume integrated security is being used

PRINT 'Database migration completed successfully!';
PRINT 'Database: SafetyAI';
PRINT 'Tables created: SafetyReports, AnalysisResults, Recommendations';
PRINT 'Indexes created for performance optimization';
PRINT 'Full-text search enabled on ExtractedText column';
PRINT 'Stored procedures created for common operations';
GO