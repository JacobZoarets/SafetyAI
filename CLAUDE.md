# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SafetyAI is a comprehensive safety reporting analysis web application that integrates with the existing DATwise EHS platform. The system leverages Google Gemini 2.5's multimodal AI capabilities to automatically process, analyze, and categorize safety incident reports from multiple input sources including documents, images, and audio. The primary objective is to transform manual safety incident processing from a 2-3 day manual workflow into a 30-second automated intelligent analysis system, achieving 90% reduction in processing time while improving accuracy and generating actionable safety recommendations.

## Build and Development Commands

### Building the Solution
- Open `SafetyAI.sln` in Visual Studio 2017 or later
- Build using Visual Studio: Build → Build Solution (Ctrl+Shift+B)
- Command line: `MSBuild SafetyAI.sln /p:Configuration=Release`

### Running Tests
- Use Visual Studio Test Explorer or MSTest runner
- Test project: `SafetyAI.Tests` with MSTest framework
- Run all tests: Test → Run All Tests in Visual Studio
- Command line: `VSTest.Console.exe SafetyAI.Tests\bin\Debug\SafetyAI.Tests.dll`

### Running the Application
- Set `SafetyAI.Web` as startup project in Visual Studio
- Run with F5 or Ctrl+F5
- Default URL: `http://localhost:8080/`
- Requires IIS Express or IIS

## Architecture Overview

### Technology Stack
- **Framework**: .NET Framework 4.8, ASP.NET Web Forms
- **Database**: Entity Framework 6.4 with SQL Server
- **AI Integration**: Google Gemini 2.5 API for multimodal processing
- **Frontend**: Bootstrap 5.3, JavaScript ES6+

### Project Structure
- **SafetyAI.Models**: Entities, DTOs, and enums for data models
- **SafetyAI.Data**: Entity Framework DbContext, repositories, and data access layer
- **SafetyAI.Services**: Business logic services including Gemini API integration
- **SafetyAI.Web**: ASP.NET Web Forms application with UI pages
- **SafetyAI.Tests**: MSTest unit tests for all layers

### Core Architecture Patterns
- **Repository Pattern**: Data access abstraction with `IRepository<T>` and specific repositories
- **Unit of Work**: Transaction management via `IUnitOfWork` 
- **Dependency Injection**: Service registration in `DependencyConfig.cs`
- **Service Layer**: Business logic separation with interface-based services

### Key Services
- **GeminiAPIClient**: Multimodal AI processing (documents, images, audio, chat)
- **DocumentProcessor**: File validation and content extraction
- **SafetyAnalyzer**: Safety incident analysis and risk assessment
- **AudioProcessor**: Audio transcription and safety term extraction

### Database Design
- **SafetyReport**: Main entity for uploaded safety documents
- **AnalysisResult**: AI analysis results linked to safety reports  
- **Recommendation**: Safety recommendations generated from analysis
- Optimized with composite indexes for common query patterns

## Configuration Requirements

### Database Connection
Update `Web.config` connectionString:
```xml
<add name="SafetyAIConnection" 
     connectionString="Data Source=YOUR_SERVER;Initial Catalog=SafetyAI;Integrated Security=True;MultipleActiveResultSets=True" 
     providerName="System.Data.SqlClient" />
```

### Gemini API Configuration
Set API key in `Web.config`:
```xml
<add key="GeminiAPIKey" value="YOUR_GEMINI_API_KEY_HERE" />
```

### File Processing Limits
- Max file size: 10MB (configurable via `MaxFileSize` app setting)
- Supported formats: PDF, JPEG, PNG, TIFF, WAV, MP3, M4A, OGG
- Processing timeout: 30 seconds (configurable)

## Key Implementation Details

### Entity Framework Configuration
- Code-First approach with automatic migrations
- DbContext: `SafetyAIDbContext` in SafetyAI.Data.Context
- Connection string name: "SafetyAIConnection"

### Error Handling
- Custom exceptions in `SafetyAI.Services.Exceptions`
- Retry policies with exponential backoff for API calls
- Comprehensive logging via `Logger` class

### Security Features
- Windows Authentication enabled
- Role-based authorization module
- Security headers configured (CSP, HSTS, X-Frame-Options)
- File validation with header verification
- Input sanitization and SQL injection protection

### Testing Strategy
- Unit tests cover data access, service logic, and API integration
- Mock implementations for external dependencies
- Test database context for isolation
- Performance tests for critical operations

## Key Functional Requirements

### Document Processing (Requirement 1)
- Support for JPEG, PNG, PDF, TIFF files up to 10MB
- Text extraction with 95% accuracy for printed text, 90% for handwritten
- Multi-language support: Hebrew, English, Arabic, Russian
- Processing completion within 30 seconds
- Confidence scoring for all extracted content

### Safety Intelligence Analysis (Requirement 2)
- Automatic incident classification (falls, slips, equipment failure, chemical exposure, near-miss)
- Severity assessment (Critical, High, Medium, Low) with 1-10 risk scoring
- Actionable safety recommendations with 90% relevance score
- Compliance mapping to OSHA, ISO 45001, local regulations
- JSON structured output for system integration

### Web Application Interface (Requirement 3)
- Responsive design for desktop, tablet, mobile devices
- Drag-and-drop file upload with progress indicators
- Real-time processing status with estimated completion times
- Color-coded severity indicators in results display
- User-friendly error messages with resolution suggestions

### Interactive Chat Interface (Requirement 5)
- Natural language queries using Gemini 2.5
- Conversation context management for multi-turn discussions
- Real-time access to safety procedures and regulatory guidelines
- Escalation pathways for human expertise when needed

### Audio Processing (Requirement 6)
- Multi-language speech recognition (Hebrew, English, Arabic, Russian)
- Noise filtering and quality enhancement
- Voice command interface for hands-free navigation
- Safety terminology recognition in transcriptions

### Performance Requirements (Requirement 10)
- Document processing: 30 seconds for files under 10MB
- Concurrent users: Support up to 100 simultaneous interactions
- API response times: Under 3 seconds for standard analysis
- System availability: 99.9% uptime during business hours
- Database queries: Sub-second response for common operations

## Implementation Status

Based on the task completion status in `.kiro/specs/safetyai-integration/tasks.md`:

### Completed Components ✅
- Project structure and core interfaces
- Database foundation with Entity Framework models
- Gemini API integration service
- Document processing service
- Safety intelligence analysis service
- Web application user interface
- Interactive chat interface
- Audio processing capabilities
- Historical analytics and reporting
- RESTful API endpoints
- Security and authentication
- Performance optimization and caching
- Error handling system
- Deployment and monitoring configuration

### In Progress/Pending Components 🔄
- Automated testing suite (Task 14)
- DATwise platform integration (Task 16)
- Final system validation and optimization (Task 17)

## Integration with DATwise Platform

The system is designed to integrate with the existing DATwise EHS platform through:
- Employee database access for user management
- Equipment maintenance system correlation
- Training record integration for competency verification
- Workflow engine integration for automated task assignment
- Single sign-on (SSO) integration for seamless authentication