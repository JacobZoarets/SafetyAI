# SafetyAI Development Prompt

## System Overview
Develop a comprehensive safety reporting analysis web application called "SafetyAI" that integrates with the existing DATwise EHS platform. The system uses Google Gemini 2.5's multimodal AI capabilities to automatically process, analyze, and categorize safety incident reports from multiple input sources including documents, images, and audio.

## Core Business Requirements

### Primary Objective
Transform manual safety incident processing from a 2-3 day manual workflow into a 30-second automated intelligent analysis system, achieving 90% reduction in processing time while improving accuracy and generating actionable safety recommendations.

### Value Proposition
- Process safety reports 90% faster than current manual methods
- Generate $2.5M annual savings per facility through prevention and optimization
- Provide proactive incident prediction and prevention capabilities
- Ensure regulatory compliance with automated documentation
- Seamlessly enhance existing DATwise EHS platform without infrastructure overhaul

## Technical Architecture Requirements

### Technology Stack
- **Frontend:** ASP.NET Web Forms 4.8 with Bootstrap 5.3 responsive design
- **Backend:** C# .NET Framework 4.8 with Entity Framework 6.4
- **Database:** Microsoft SQL Server 2019+ with full-text search capabilities
- **AI Processing:** Google Gemini 2.5 API for all multimodal AI operations
- **Authentication:** Active Directory integration with role-based access control
- **Hosting:** Windows Server 2019+ with IIS 10+

### Core System Components
1. **Gemini Multimodal Analysis Engine** - Central AI processing hub
2. **Responsive Web Interface** - User interaction and data presentation
3. **Data Management System** - SQL Server database with optimized schema
4. **RESTful API Layer** - Integration endpoints for external systems
5. **Security Framework** - Authentication, authorization, and data protection

## Development Priority Structure

### Priority 1: MVP Core Features (Critical - Weeks 1-2)
**Objective:** Deliver functional demo meeting basic requirements

#### 1.1 Gemini Document Understanding Module
- Accept JPEG, PNG, PDF, TIFF file uploads up to 10MB
- Integrate Google Gemini 2.5 document understanding for text extraction
- Process handwritten and printed text in Hebrew, English, Arabic, Russian
- Achieve 95% accuracy for printed text, 90% for handwritten content
- Complete processing within 30 seconds for standard documents
- Provide confidence scoring for all extracted content

#### 1.2 Gemini Safety Intelligence Module  
- Analyze extracted text using Gemini 2.5's natural language understanding
- Classify incidents into standard categories (falls, slips, equipment failure, chemical exposure, near-miss)
- Assess severity levels (Critical, High, Medium, Low) with 1-10 risk scoring
- Generate specific, actionable safety recommendations
- Map findings to relevant safety standards (OSHA, ISO 45001, local regulations)
- Output structured JSON results for system integration

#### 1.3 Web Application Interface
- Create responsive design supporting desktop, tablet, and mobile devices
- Implement drag-and-drop file upload with progress indicators
- Display real-time processing status with estimated completion times
- Present analysis results with color-coded severity indicators
- Show extracted text, incident classification, and recommendations
- Include file validation with clear error messages for unsupported formats

#### 1.4 Database Foundation
- Design SQL Server schema supporting safety reports, analysis results, and recommendations
- Implement core tables: SafetyReports, AnalysisResults, Recommendations, UserActivity
- Create appropriate indexes for performance optimization
- Include full-text search capabilities for content queries
- Establish audit trail logging for all system interactions

### Priority 2: Enhanced Features (Important - Weeks 3-4)
**Objective:** Improve user experience and add interactive capabilities

#### 2.1 Interactive Chat Interface
- Implement conversational AI using Gemini 2.5 for safety consultation
- Process natural language queries about safety protocols and regulations
- Maintain conversation context for complex multi-turn discussions
- Provide real-time access to safety procedures and regulatory guidelines
- Enable interactive guidance through incident response protocols
- Include escalation pathways for complex situations requiring human expertise

#### 2.2 Enhanced User Interface
- Develop comprehensive dashboard with recent activity and system status
- Create results display with executive summary and detailed analysis views
- Implement notification system for urgent recommendations and alerts
- Add export functionality for sharing results with stakeholders
- Include historical incident grid with search and filtering capabilities
- Optimize performance with caching for frequently accessed data

#### 2.3 Advanced Integration
- Establish RESTful API endpoints for external system integration
- Implement authentication through Active Directory with role-based permissions
- Create automated workflow triggers for task assignment and notifications
- Add comprehensive error handling with user-friendly messaging
- Include system performance monitoring and logging capabilities

### Priority 3: Advanced Features (Optional - Weeks 5+)
**Objective:** Add sophisticated capabilities for comprehensive safety intelligence

#### 3.1 Gemini Audio Processing
- Integrate Gemini 2.5 audio understanding for hands-free incident reporting
- Support multi-language speech recognition for primary organizational languages
- Implement intelligent audio processing with automatic noise filtering
- Provide real-time transcription with integrated safety terminology recognition
- Enable voice command interface for hands-free system navigation

#### 3.2 Historical Analytics Engine
- Analyze accumulated safety data to identify trends and patterns
- Implement temporal analysis for seasonal risk identification
- Create spatial analysis revealing location-based risk concentrations
- Develop correlation analysis examining incident relationships with environmental factors
- Build predictive modeling for future incident probability forecasting
- Generate cost-benefit analysis for proposed safety improvements

## Data Schema Requirements

### Core Database Tables
```sql
SafetyReports Table:
- Id (UNIQUEIDENTIFIER, Primary Key)
- FileName (NVARCHAR(255), Required)
- FileSize (INT)
- FileType (NVARCHAR(50))
- OriginalText (NVARCHAR(MAX))
- ExtractedText (NVARCHAR(MAX))
- ProcessingStatus (NVARCHAR(50), Default: 'Pending')
- UploadedBy (NVARCHAR(100))
- UploadedDate (DATETIME2, Default: GETUTCDATE())
- ProcessedDate (DATETIME2)
- IsActive (BIT, Default: 1)

AnalysisResults Table:
- Id (UNIQUEIDENTIFIER, Primary Key)
- ReportId (UNIQUEIDENTIFIER, Foreign Key)
- IncidentType (NVARCHAR(100))
- Severity (NVARCHAR(50))
- RiskScore (INT, 1-10 scale)
- Summary (NVARCHAR(500))
- AnalysisDate (DATETIME2)
- ConfidenceLevel (DECIMAL(4,3))
- ProcessingTimeMs (INT)
- AIModel (NVARCHAR(100))

Recommendations Table:
- Id (UNIQUEIDENTIFIER, Primary Key)
- AnalysisId (UNIQUEIDENTIFIER, Foreign Key)
- RecommendationType (NVARCHAR(100))
- Description (NVARCHAR(MAX))
- Priority (NVARCHAR(50))
- EstimatedCost (DECIMAL(10,2))
- EstimatedTimeHours (INT)
- ResponsibleRole (NVARCHAR(100))
- Status (NVARCHAR(50), Default: 'Pending')
```

## API Integration Specifications

### Google Gemini 2.5 Integration
- Implement multimodal content processing supporting text, image, and audio inputs
- Use Gemini's document understanding for comprehensive text extraction
- Leverage image understanding for visual safety hazard identification
- Integrate audio understanding for voice-to-text with contextual comprehension
- Maintain API key security with encrypted configuration storage
- Implement retry logic and error handling for API availability issues

### RESTful API Endpoints
```
POST /api/v1/safety/analyze
- Accept multipart/form-data with file and metadata
- Return structured analysis results with processing status
- Include confidence scores and processing time metrics

POST /api/v1/safety/chat
- Process natural language safety consultation queries
- Maintain session context for conversation continuity
- Return responses with suggested actions and relevant documents

GET /api/v1/safety/analytics/historical
- Query historical incident data with date and location filters
- Return trend analysis, risk hotspots, and predictive insights
- Include ROI calculations and cost-benefit analysis
```

## Performance and Quality Requirements

### System Performance Metrics
- Document processing completion within 30 seconds for files under 10MB
- Concurrent user support for up to 100 simultaneous interactions
- API response times under 3 seconds for standard analysis requests
- System availability maintaining 99.9% uptime during business hours
- Database query optimization ensuring sub-second response for common operations

### Analysis Accuracy Standards
- Document understanding accuracy of 95% or higher for printed text
- Handwritten text recognition accuracy of 90% or higher
- Incident classification accuracy of 95% or higher
- Safety recommendation relevance score of 90% or higher
- Multi-language processing maintaining consistency across all supported languages

## Security and Compliance Framework

### Data Protection Measures
- Implement TLS 1.3 encryption for all data transmission
- Apply AES-256 encryption for data at rest in database and file storage
- Integrate Active Directory authentication with multi-factor authentication for admin access
- Establish comprehensive audit logging for all system interactions and data access
- Include data anonymization capabilities for PII protection during AI processing

### Regulatory Compliance
- Ensure GDPR compliance with data privacy controls and user rights management
- Meet OSHA standards for safety reporting and documentation requirements
- Support industry-specific regulations for hazardous material handling
- Implement data retention policies complying with organizational and legal requirements

## Integration Requirements

### DATwise Platform Integration
- Read access to existing employee database for reporter identification and context
- Integration with equipment maintenance systems for asset correlation analysis
- Access to training records for competency verification and context
- Connection to workflow engines for automated task creation and assignment
- Single sign-on integration for seamless user authentication experience

### External System Connectivity
- Weather service integration for environmental correlation analysis
- Emergency services connection for automated incident reporting when required
- Regulatory database access for compliance requirement updates
- Insurance system integration for risk assessment data sharing

## Testing and Quality Assurance

### Testing Requirements
- Unit testing with 90% or higher code coverage for all business logic
- Integration testing validating API interactions and database operations
- System testing confirming end-to-end workflow functionality
- User acceptance testing with stakeholder validation process
- Performance testing validating response times and concurrent user support

### AI Model Validation
- Regular accuracy testing against known safety incident datasets
- Performance validation for response time and throughput requirements
- Bias testing ensuring fairness and consistency across different input types
- Regression testing for model drift detection and correction protocols

## Deployment and Infrastructure

### Deployment Strategy
- Development → Staging → Production environment progression
- Automated CI/CD pipeline with comprehensive testing validation
- Database migration scripts with rollback capabilities
- Configuration management for environment-specific settings
- Monitoring and alerting system for production health tracking

### Infrastructure Requirements
- Windows Server 2019+ hosting environment with IIS 10+
- SQL Server 2019+ with appropriate licensing and backup strategies
- Load balancing capabilities for high-traffic period management
- Automated backup systems with 30-day retention policy
- Application monitoring with performance metrics and error tracking

## Success Metrics and Evaluation

### Technical Performance Indicators
- Processing time reduction achieving 90% improvement over manual analysis
- Analysis accuracy maintaining 95% correctness for incident classification
- System reliability ensuring 99.9% availability during operational hours
- User adoption reaching 80% of target safety personnel within 90 days

### Business Impact Measurements
- Cost reduction quantifying savings in processing time and improved efficiency
- Incident prevention tracking reduction in preventable safety events
- Compliance improvement measuring enhanced regulatory adherence
- Return on investment calculating total benefits versus implementation costs

## Development Guidelines

### Code Quality Standards
- Follow Microsoft C# coding conventions and best practices
- Implement comprehensive error handling with user-friendly messaging
- Use dependency injection for testable and maintainable architecture
- Include comprehensive logging for troubleshooting and performance monitoring
- Maintain clean, well-documented code with inline comments for complex logic

### User Experience Principles
- Design mobile-first responsive interfaces supporting all device types
- Implement intuitive navigation with minimal learning curve requirements
- Provide clear visual feedback for all user actions and system status
- Include accessibility features ensuring usability for users with diverse abilities
- Optimize loading times and provide progress indicators for longer operations

### Security Best Practices
- Validate all user inputs to prevent injection attacks and data corruption
- Implement proper session management with appropriate timeout policies
- Use parameterized queries for all database interactions
- Apply principle of least privilege for all system access permissions
- Include comprehensive security logging for audit and compliance purposes

---

**Implementation Notes:**
- Begin with Priority 1 features to establish working foundation
- Ensure each component is fully functional before proceeding to next priority level
- Maintain flexible architecture supporting future enhancements and scalability
- Document all design decisions and implementation details for maintenance
- Include comprehensive testing at each development phase to ensure quality delivery