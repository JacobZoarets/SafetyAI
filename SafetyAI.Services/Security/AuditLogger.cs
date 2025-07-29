using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SafetyAI.Data.Interfaces;
using SafetyAI.Models.Entities;
using SafetyAI.Services.Infrastructure;

namespace SafetyAI.Services.Security
{
    public class AuditLogger : IAuditLogger, IDisposable
    {
        private readonly IUnitOfWork _unitOfWork;
        private bool _disposed = false;

        public AuditLogger(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task LogUserActionAsync(string userId, string action, string details, string ipAddress = null)
        {
            try
            {
                var auditEntry = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = action,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    UserAgent = System.Web.HttpContext.Current?.Request?.UserAgent
                };

                // In a real implementation, you would have an AuditLog repository
                // For now, we'll log to the standard logger
                Logger.LogInfo($"AUDIT: User={userId}, Action={action}, Details={details}, IP={ipAddress}", "Audit");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to log audit entry: {ex.Message}", "AuditLogger");
            }
        }

        public async Task LogSecurityEventAsync(string eventType, string description, string userId = null, Dictionary<string, object> additionalData = null)
        {
            try
            {
                var securityEvent = new SecurityEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = eventType,
                    Description = description,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = System.Web.HttpContext.Current?.Request?.UserHostAddress,
                    AdditionalData = additionalData != null ? 
                        Newtonsoft.Json.JsonConvert.SerializeObject(additionalData) : null
                };

                Logger.LogWarning($"SECURITY: Type={eventType}, Description={description}, User={userId}", "Security");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to log security event: {ex.Message}", "AuditLogger");
            }
        }

        public async Task LogDataAccessAsync(string userId, string dataType, string operation, string recordId = null)
        {
            try
            {
                await LogUserActionAsync(userId, $"DataAccess_{operation}", 
                    $"Accessed {dataType}" + (recordId != null ? $" (ID: {recordId})" : ""));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to log data access: {ex.Message}", "AuditLogger");
            }
        }

        public async Task LogAuthenticationEventAsync(string userId, string eventType, bool success, string details = null)
        {
            try
            {
                var eventDescription = $"Authentication {eventType}: {(success ? "Success" : "Failed")}";
                if (!string.IsNullOrEmpty(details))
                {
                    eventDescription += $" - {details}";
                }

                await LogSecurityEventAsync("Authentication", eventDescription, userId);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to log authentication event: {ex.Message}", "AuditLogger");
            }
        }

        public async Task LogSystemEventAsync(string eventType, string description, Dictionary<string, object> additionalData = null)
        {
            try
            {
                Logger.LogInfo($"SYSTEM: Type={eventType}, Description={description}", "System");
                
                if (additionalData != null)
                {
                    var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(additionalData);
                    Logger.LogInfo($"SYSTEM_DATA: {dataJson}", "System");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to log system event: {ex.Message}", "AuditLogger");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _unitOfWork?.Dispose();
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

    public interface IAuditLogger
    {
        Task LogUserActionAsync(string userId, string action, string details, string ipAddress = null);
        Task LogSecurityEventAsync(string eventType, string description, string userId = null, Dictionary<string, object> additionalData = null);
        Task LogDataAccessAsync(string userId, string dataType, string operation, string recordId = null);
        Task LogAuthenticationEventAsync(string userId, string eventType, bool success, string details = null);
        Task LogSystemEventAsync(string eventType, string description, Dictionary<string, object> additionalData = null);
    }

    // Placeholder entities for audit logging
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SecurityEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public string AdditionalData { get; set; }
    }
}