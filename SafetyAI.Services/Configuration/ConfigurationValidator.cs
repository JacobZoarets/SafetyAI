using System;
using System.Collections.Generic;
using System.Configuration;
using SafetyAI.Services.Exceptions;

namespace SafetyAI.Services.Configuration
{
    public static class ConfigurationValidator
    {
        public static void ValidateConfiguration()
        {
            var errors = new List<string>();

            // Validate Gemini API configuration
            if (string.IsNullOrEmpty(ServiceConfiguration.GeminiAPIKey))
            {
                errors.Add("GeminiAPIKey is required but not configured in app settings.");
            }

            if (string.IsNullOrEmpty(ServiceConfiguration.GeminiAPIEndpoint))
            {
                errors.Add("GeminiAPIEndpoint is required but not configured in app settings.");
            }

            // Validate file processing configuration
            if (ServiceConfiguration.MaxFileSize <= 0)
            {
                errors.Add("MaxFileSize must be greater than 0.");
            }

            if (ServiceConfiguration.ProcessingTimeoutSeconds <= 0)
            {
                errors.Add("ProcessingTimeoutSeconds must be greater than 0.");
            }

            // Validate retry configuration
            if (ServiceConfiguration.RetryCount < 0)
            {
                errors.Add("RetryCount must be 0 or greater.");
            }

            if (ServiceConfiguration.RetryDelaySeconds <= 0)
            {
                errors.Add("RetryDelaySeconds must be greater than 0.");
            }

            // Validate supported file types
            if (ServiceConfiguration.SupportedFileTypes == null || ServiceConfiguration.SupportedFileTypes.Length == 0)
            {
                errors.Add("SupportedFileTypes must be configured with at least one file type.");
            }

            // Validate supported languages
            if (ServiceConfiguration.SupportedLanguages == null || ServiceConfiguration.SupportedLanguages.Length == 0)
            {
                errors.Add("SupportedLanguages must be configured with at least one language.");
            }

            if (errors.Count > 0)
            {
                var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors);
                throw new SafetyAI.Services.Exceptions.ConfigurationException(errorMessage);
            }
        }

        public static bool IsConfigurationValid()
        {
            try
            {
                ValidateConfiguration();
                return true;
            }
            catch (SafetyAI.Services.Exceptions.ConfigurationException)
            {
                return false;
            }
        }

        public static ConfigurationStatus GetConfigurationStatus()
        {
            var status = new ConfigurationStatus();

            try
            {
                ValidateConfiguration();
                status.IsValid = true;
                status.Message = "Configuration is valid.";
            }
            catch (SafetyAI.Services.Exceptions.ConfigurationException ex)
            {
                status.IsValid = false;
                status.Message = ex.Message;
                status.Errors = ex.Message.Split('\n');
            }

            // Add configuration details
            status.Details = new Dictionary<string, object>
            {
                ["GeminiAPIKey"] = string.IsNullOrEmpty(ServiceConfiguration.GeminiAPIKey) ? "Not configured" : "Configured",
                ["GeminiAPIEndpoint"] = ServiceConfiguration.GeminiAPIEndpoint,
                ["MaxFileSize"] = ServiceConfiguration.MaxFileSize,
                ["ProcessingTimeoutSeconds"] = ServiceConfiguration.ProcessingTimeoutSeconds,
                ["RetryCount"] = ServiceConfiguration.RetryCount,
                ["RetryDelaySeconds"] = ServiceConfiguration.RetryDelaySeconds,
                ["SupportedFileTypes"] = ServiceConfiguration.SupportedFileTypes,
                ["SupportedAudioTypes"] = ServiceConfiguration.SupportedAudioTypes,
                ["SupportedLanguages"] = ServiceConfiguration.SupportedLanguages
            };

            return status;
        }
    }

    public class ConfigurationStatus
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string[] Errors { get; set; }
        public Dictionary<string, object> Details { get; set; }

        public ConfigurationStatus()
        {
            Details = new Dictionary<string, object>();
        }
    }
}