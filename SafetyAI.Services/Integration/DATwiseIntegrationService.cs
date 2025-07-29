using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SafetyAI.Models.DTOs;
using SafetyAI.Services.Infrastructure;
using SafetyAI.Services.Interfaces;

namespace SafetyAI.Services.Integration
{
    public class DATwiseIntegrationService : IDATwiseIntegrationService, IDisposable
    {
        private readonly IDATwiseApiClient _datwiseApiClient;
        private readonly IEmployeeService _employeeService;
        private readonly IEquipmentService _equipmentService;
        private readonly ITrainingService _trainingService;
        private readonly IWorkflowService _workflowService;
        private bool _disposed = false;

        public DATwiseIntegrationService(
            IDATwiseApiClient datwiseApiClient,
            IEmployeeService employeeService,
            IEquipmentService equipmentService,
            ITrainingService trainingService,
            IWorkflowService workflowService)
        {
            _datwiseApiClient = datwiseApiClient ?? throw new ArgumentNullException(nameof(datwiseApiClient));
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _equipmentService = equipmentService ?? throw new ArgumentNullException(nameof(equipmentService));
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        }

        public async Task<EmployeeProfile> GetEmployeeProfileAsync(string employeeId)
        {
            using (var context = new LogContext($"GetEmployeeProfile_{employeeId}"))
            {
                try
                {
                    context.LogProgress($"Retrieving employee profile for ID: {employeeId}");

                    var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
                    if (employee == null)
                    {
                        context.LogWarning($"Employee not found: {employeeId}");
                        return null;
                    }

                    // Get additional employee data from DATwise
                    var datwiseEmployee = await _datwiseApiClient.GetEmployeeAsync(employeeId);
                    
                    // Get training records
                    var trainingRecords = await _trainingService.GetEmployeeTrainingRecordsAsync(employeeId);
                    
                    // Get safety certifications
                    var certifications = await _trainingService.GetEmployeeCertificationsAsync(employeeId);

                    var profile = new EmployeeProfile
                    {
                        EmployeeId = employee.Id,
                        FirstName = employee.FirstName,
                        LastName = employee.LastName,
                        Email = employee.Email,
                        Department = employee.Department,
                        JobTitle = employee.JobTitle,
                        HireDate = employee.HireDate,
                        SupervisorId = employee.SupervisorId,
                        Location = employee.WorkLocation,
                        SafetyRole = employee.SafetyRole,
                        TrainingRecords = trainingRecords?.ToList() ?? new List<TrainingRecord>(),
                        Certifications = certifications?.ToList() ?? new List<SafetyCertification>(),
                        LastSafetyTrainingDate = trainingRecords?.Where(t => t.TrainingType.Contains("Safety"))
                            .OrderByDescending(t => t.CompletionDate)
                            .FirstOrDefault()?.CompletionDate,
                        CompetencyLevel = CalculateCompetencyLevel(trainingRecords, certifications),
                        IsActive = employee.IsActive
                    };

                    context.LogProgress($"Employee profile retrieved successfully for {employee.FirstName} {employee.LastName}");
                    return profile;
                }
                catch (Exception ex)
                {
                    context.LogError($"Failed to retrieve employee profile: {ex.Message}");
                    Logger.LogError(ex, "DATwiseIntegration");
                    throw;
                }
            }
        }

        public async Task<List<EquipmentAsset>> GetEquipmentAssetsAsync(string locationId = null)
        {
            using (var context = new LogContext("GetEquipmentAssets"))
            {
                try
                {
                    context.LogProgress($"Retrieving equipment assets for location: {locationId ?? "All"}");

                    var equipment = await _equipmentService.GetEquipmentAsync(locationId);
                    var assets = new List<EquipmentAsset>();

                    foreach (var item in equipment)
                    {
                        // Get maintenance history from DATwise
                        var maintenanceHistory = await _datwiseApiClient.GetEquipmentMaintenanceHistoryAsync(item.Id);
                        
                        // Get current maintenance status
                        var maintenanceStatus = await _equipmentService.GetMaintenanceStatusAsync(item.Id);

                        var asset = new EquipmentAsset
                        {
                            AssetId = item.Id,
                            AssetTag = item.AssetTag,
                            Name = item.Name,
                            Description = item.Description,
                            Category = item.Category,
                            Manufacturer = item.Manufacturer,
                            Model = item.Model,
                            SerialNumber = item.SerialNumber,
                            Location = item.Location,
                            InstallationDate = item.InstallationDate,
                            LastMaintenanceDate = maintenanceHistory?.OrderByDescending(m => m.Date).FirstOrDefault()?.Date,
                            NextMaintenanceDate = maintenanceStatus?.NextScheduledMaintenance,
                            MaintenanceStatus = maintenanceStatus?.Status ?? "Unknown",
                            SafetyRating = item.SafetyRating,
                            RiskLevel = CalculateEquipmentRiskLevel(item, maintenanceHistory),
                            MaintenanceHistory = maintenanceHistory?.ToList() ?? new List<MaintenanceRecord>(),
                            IsActive = item.IsActive
                        };

                        assets.Add(asset);
                    }

                    context.LogProgress($"Retrieved {assets.Count} equipment assets");
                    return assets;
                }
                catch (Exception ex)
                {
                    context.LogError($"Failed to retrieve equipment assets: {ex.Message}");
                    Logger.LogError(ex, "DATwiseIntegration");
                    throw;
                }
            }
        }

        public async Task<List<TrainingRecord>> GetTrainingRecordsAsync(string employeeId)
        {
            using (var context = new LogContext($"GetTrainingRecords_{employeeId}"))
            {
                try
                {
                    context.LogProgress($"Retrieving training records for employee: {employeeId}");

                    var records = await _trainingService.GetEmployeeTrainingRecordsAsync(employeeId);
                    
                    // Enhance with DATwise data
                    var enhancedRecords = new List<TrainingRecord>();
                    foreach (var record in records)
                    {
                        var datwiseRecord = await _datwiseApiClient.GetTrainingRecordDetailsAsync(record.Id);
                        
                        var enhancedRecord = new TrainingRecord
                        {
                            Id = record.Id,
                            EmployeeId = record.EmployeeId,
                            TrainingType = record.TrainingType,
                            TrainingTitle = record.TrainingTitle,
                            Description = record.Description,
                            Provider = record.Provider,
                            CompletionDate = record.CompletionDate,
                            ExpirationDate = record.ExpirationDate,
                            Score = record.Score,
                            Status = record.Status,
                            CertificationNumber = record.CertificationNumber,
                            InstructorName = datwiseRecord?.InstructorName,
                            TrainingHours = datwiseRecord?.TrainingHours ?? 0,
                            CompetencyAchieved = record.Score >= 80, // 80% passing score
                            RequiresRenewal = record.ExpirationDate.HasValue && 
                                            record.ExpirationDate.Value <= DateTime.UtcNow.AddDays(30)
                        };

                        enhancedRecords.Add(enhancedRecord);
                    }

                    context.LogProgress($"Retrieved {enhancedRecords.Count} training records");
                    return enhancedRecords;
                }
                catch (Exception ex)
                {
                    context.LogError($"Failed to retrieve training records: {ex.Message}");
                    Logger.LogError(ex, "DATwiseIntegration");
                    throw;
                }
            }
        }

        public async Task<WorkflowTask> CreateSafetyWorkflowTaskAsync(SafetyIncidentWorkflowRequest request)
        {
            using (var context = new LogContext("CreateSafetyWorkflowTask"))
            {
                try
                {
                    context.LogProgress($"Creating safety workflow task for incident: {request.IncidentId}");

                    // Determine task assignment based on incident severity and type
                    var assignee = await DetermineTaskAssigneeAsync(request);
                    
                    // Create workflow task in DATwise
                    var workflowTask = new WorkflowTask
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = $"Safety Incident Response - {request.IncidentType}",
                        Description = GenerateTaskDescription(request),
                        Priority = MapSeverityToPriority(request.Severity),
                        AssignedTo = assignee.EmployeeId,
                        AssignedToName = $"{assignee.FirstName} {assignee.LastName}",
                        CreatedBy = request.CreatedBy,
                        CreatedDate = DateTime.UtcNow,
                        DueDate = CalculateDueDate(request.Severity),
                        Status = "Open",
                        Category = "Safety",
                        IncidentId = request.IncidentId,
                        RequiredActions = GenerateRequiredActions(request),
                        EstimatedHours = EstimateTaskHours(request),
                        Department = assignee.Department
                    };

                    // Save to DATwise workflow system
                    var createdTask = await _workflowService.CreateTaskAsync(workflowTask);
                    
                    // Send notifications
                    await SendTaskNotificationAsync(createdTask, assignee);
                    
                    // Create follow-up tasks if needed
                    if (request.Severity == "Critical" || request.Severity == "High")
                    {
                        await CreateFollowUpTasksAsync(request, createdTask);
                    }

                    context.LogProgress($"Safety workflow task created successfully: {createdTask.Id}");
                    return createdTask;
                }
                catch (Exception ex)
                {
                    context.LogError($"Failed to create safety workflow task: {ex.Message}");
                    Logger.LogError(ex, "DATwiseIntegration");
                    throw;
                }
            }
        }

        public async Task<SingleSignOnResult> AuthenticateUserAsync(string username, string domain = null)
        {
            using (var context = new LogContext($"AuthenticateUser_{username}"))
            {
                try
                {
                    context.LogProgress($"Authenticating user: {username}");

                    // Authenticate with DATwise SSO
                    var authResult = await _datwiseApiClient.AuthenticateAsync(username, domain);
                    
                    if (!authResult.IsSuccess)
                    {
                        context.LogWarning($"Authentication failed for user: {username}");
                        return new SingleSignOnResult
                        {
                            IsSuccess = false,
                            ErrorMessage = authResult.ErrorMessage,
                            Username = username
                        };
                    }

                    // Get user profile and permissions
                    var userProfile = await GetEmployeeProfileAsync(authResult.EmployeeId);
                    var permissions = await _datwiseApiClient.GetUserPermissionsAsync(authResult.EmployeeId);

                    var ssoResult = new SingleSignOnResult
                    {
                        IsSuccess = true,
                        Username = username,
                        EmployeeId = authResult.EmployeeId,
                        DisplayName = $"{userProfile?.FirstName} {userProfile?.LastName}",
                        Email = userProfile?.Email,
                        Department = userProfile?.Department,
                        JobTitle = userProfile?.JobTitle,
                        Roles = MapDATwiseRolesToSafetyAIRoles(permissions),
                        Permissions = permissions?.ToList() ?? new List<string>(),
                        SessionToken = authResult.SessionToken,
                        TokenExpiration = authResult.TokenExpiration
                    };

                    context.LogProgress($"User authenticated successfully: {ssoResult.DisplayName}");
                    return ssoResult;
                }
                catch (Exception ex)
                {
                    context.LogError($"Authentication failed: {ex.Message}");
                    Logger.LogError(ex, "DATwiseIntegration");
                    
                    return new SingleSignOnResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Authentication service unavailable",
                        Username = username
                    };
                }
            }
        }

        public async Task<bool> ValidateUserPermissionAsync(string employeeId, string permission)
        {
            try
            {
                var permissions = await _datwiseApiClient.GetUserPermissionsAsync(employeeId);
                return permissions?.Contains(permission) == true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to validate user permission: {ex.Message}", "DATwiseIntegration");
                return false; // Fail closed for security
            }
        }

        private string CalculateCompetencyLevel(IEnumerable<TrainingRecord> trainingRecords, IEnumerable<SafetyCertification> certifications)
        {
            try
            {
                var recentTraining = trainingRecords?.Where(t => t.CompletionDate >= DateTime.UtcNow.AddYears(-2)).Count() ?? 0;
                var activeCertifications = certifications?.Where(c => c.ExpirationDate > DateTime.UtcNow).Count() ?? 0;
                var averageScore = trainingRecords?.Where(t => t.Score.HasValue).Average(t => t.Score.Value) ?? 0;

                if (activeCertifications >= 3 && recentTraining >= 5 && averageScore >= 90)
                    return "Expert";
                else if (activeCertifications >= 2 && recentTraining >= 3 && averageScore >= 80)
                    return "Advanced";
                else if (activeCertifications >= 1 && recentTraining >= 2 && averageScore >= 70)
                    return "Intermediate";
                else
                    return "Basic";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string CalculateEquipmentRiskLevel(Equipment equipment, IEnumerable<MaintenanceRecord> maintenanceHistory)
        {
            try
            {
                var daysSinceLastMaintenance = maintenanceHistory?.Any() == true ?
                    (DateTime.UtcNow - maintenanceHistory.OrderByDescending(m => m.Date).First().Date).Days : 365;

                var maintenanceFrequency = maintenanceHistory?.Count(m => m.Date >= DateTime.UtcNow.AddYears(-1)) ?? 0;

                if (daysSinceLastMaintenance > 180 || maintenanceFrequency == 0)
                    return "High";
                else if (daysSinceLastMaintenance > 90 || maintenanceFrequency < 2)
                    return "Medium";
                else
                    return "Low";
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task<EmployeeProfile> DetermineTaskAssigneeAsync(SafetyIncidentWorkflowRequest request)
        {
            // Logic to determine who should be assigned the task based on incident type and severity
            switch (request.IncidentType.ToLowerInvariant())
            {
                case "chemical":
                case "chemicalexposure":
                    return await _employeeService.GetEmployeeByRoleAsync("Chemical Safety Officer");
                
                case "fire":
                    return await _employeeService.GetEmployeeByRoleAsync("Fire Safety Manager");
                
                case "equipment":
                case "equipmentfailure":
                    return await _employeeService.GetEmployeeByRoleAsync("Maintenance Supervisor");
                
                default:
                    return await _employeeService.GetEmployeeByRoleAsync("Safety Manager");
            }
        }

        private string GenerateTaskDescription(SafetyIncidentWorkflowRequest request)
        {
            return $"A {request.Severity.ToLower()} severity {request.IncidentType.ToLower()} incident has been reported and requires immediate attention.\n\n" +
                   $"Incident Summary: {request.Summary}\n\n" +
                   $"Risk Score: {request.RiskScore}/10\n\n" +
                   $"Please review the incident details, conduct necessary investigations, and implement appropriate corrective actions.";
        }

        private string MapSeverityToPriority(string severity)
        {
            return severity?.ToLowerInvariant() switch
            {
                "critical" => "Critical",
                "high" => "High",
                "medium" => "Medium",
                "low" => "Low",
                _ => "Medium"
            };
        }

        private DateTime CalculateDueDate(string severity)
        {
            var hoursToAdd = severity?.ToLowerInvariant() switch
            {
                "critical" => 4,   // 4 hours
                "high" => 24,      // 1 day
                "medium" => 72,    // 3 days
                "low" => 168,      // 1 week
                _ => 48            // 2 days default
            };

            return DateTime.UtcNow.AddHours(hoursToAdd);
        }

        private List<string> GenerateRequiredActions(SafetyIncidentWorkflowRequest request)
        {
            var actions = new List<string>
            {
                "Review incident report and supporting documentation",
                "Conduct on-site investigation if required",
                "Interview involved personnel",
                "Identify root causes and contributing factors",
                "Develop corrective action plan",
                "Implement immediate safety measures",
                "Update safety procedures if necessary",
                "Document findings and actions taken"
            };

            // Add severity-specific actions
            if (request.Severity == "Critical")
            {
                actions.Insert(0, "Notify senior management immediately");
                actions.Insert(1, "Ensure area is secured and safe");
            }

            return actions;
        }

        private int EstimateTaskHours(SafetyIncidentWorkflowRequest request)
        {
            return request.Severity?.ToLowerInvariant() switch
            {
                "critical" => 16,  // 2 days
                "high" => 8,       // 1 day
                "medium" => 4,     // Half day
                "low" => 2,        // 2 hours
                _ => 4             // Default
            };
        }

        private async Task CreateFollowUpTasksAsync(SafetyIncidentWorkflowRequest request, WorkflowTask parentTask)
        {
            // Create follow-up tasks for high-severity incidents
            var followUpTasks = new[]
            {
                new WorkflowTask
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Safety Incident Follow-up Review",
                    Description = "Conduct 30-day follow-up review to ensure corrective actions are effective",
                    Priority = "Medium",
                    AssignedTo = parentTask.AssignedTo,
                    CreatedBy = request.CreatedBy,
                    CreatedDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    Status = "Scheduled",
                    Category = "Safety Follow-up",
                    ParentTaskId = parentTask.Id
                }
            };

            foreach (var task in followUpTasks)
            {
                await _workflowService.CreateTaskAsync(task);
            }
        }

        private async Task SendTaskNotificationAsync(WorkflowTask task, EmployeeProfile assignee)
        {
            try
            {
                // Send notification through DATwise notification system
                await _datwiseApiClient.SendNotificationAsync(new NotificationRequest
                {
                    RecipientId = assignee.EmployeeId,
                    Subject = $"New Safety Task Assigned: {task.Title}",
                    Message = $"You have been assigned a new safety task with {task.Priority.ToLower()} priority. Due date: {task.DueDate:yyyy-MM-dd HH:mm}",
                    Category = "Safety",
                    Priority = task.Priority
                });
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to send task notification: {ex.Message}", "DATwiseIntegration");
            }
        }

        private List<string> MapDATwiseRolesToSafetyAIRoles(IEnumerable<string> datwisePermissions)
        {
            var roles = new List<string>();

            if (datwisePermissions?.Contains("ADMIN") == true)
                roles.Add("Administrator");
            
            if (datwisePermissions?.Contains("SAFETY_MANAGER") == true)
                roles.Add("SafetyManager");
            
            if (datwisePermissions?.Contains("SUPERVISOR") == true)
                roles.Add("Supervisor");
            
            if (roles.Count == 0)
                roles.Add("Employee");

            return roles;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _datwiseApiClient?.Dispose();
                    _employeeService?.Dispose();
                    _equipmentService?.Dispose();
                    _trainingService?.Dispose();
                    _workflowService?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}