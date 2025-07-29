using System;

namespace SafetyAI.Services.Exceptions
{
    public class SafetyAIException : Exception
    {
        public string ErrorCode { get; }
        public object ErrorDetails { get; }

        public SafetyAIException(string message) : base(message)
        {
        }

        public SafetyAIException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public SafetyAIException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public SafetyAIException(string message, string errorCode, object errorDetails) : base(message)
        {
            ErrorCode = errorCode;
            ErrorDetails = errorDetails;
        }

        public SafetyAIException(string message, string errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    public class GeminiAPIException : SafetyAIException
    {
        public int? HttpStatusCode { get; }
        public string GeminiErrorCode { get; }

        public GeminiAPIException(string message) : base(message)
        {
        }

        public GeminiAPIException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public GeminiAPIException(string message, int httpStatusCode, string geminiErrorCode) : base(message)
        {
            HttpStatusCode = httpStatusCode;
            GeminiErrorCode = geminiErrorCode;
        }

        public GeminiAPIException(string message, int httpStatusCode, string geminiErrorCode, Exception innerException) 
            : base(message, innerException)
        {
            HttpStatusCode = httpStatusCode;
            GeminiErrorCode = geminiErrorCode;
        }
    }

    public class FileValidationException : SafetyAIException
    {
        public string FileName { get; }
        public long? FileSize { get; }
        public string FileType { get; }

        public FileValidationException(string message) : base(message)
        {
        }

        public FileValidationException(string message, string fileName) : base(message)
        {
            FileName = fileName;
        }

        public FileValidationException(string message, string fileName, long fileSize, string fileType) : base(message)
        {
            FileName = fileName;
            FileSize = fileSize;
            FileType = fileType;
        }

        public FileValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ProcessingException : SafetyAIException
    {
        public TimeSpan? ProcessingTime { get; }
        public string ProcessingStage { get; }

        public ProcessingException(string message) : base(message)
        {
        }

        public ProcessingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ProcessingException(string message, string processingStage) : base(message)
        {
            ProcessingStage = processingStage;
        }

        public ProcessingException(string message, string processingStage, TimeSpan processingTime) : base(message)
        {
            ProcessingStage = processingStage;
            ProcessingTime = processingTime;
        }

        public ProcessingException(string message, string processingStage, Exception innerException) 
            : base(message, innerException)
        {
            ProcessingStage = processingStage;
        }
    }

    public class ConfigurationException : SafetyAIException
    {
        public string ConfigurationKey { get; }

        public ConfigurationException(string message) : base(message)
        {
        }

        public ConfigurationException(string message, string configurationKey) : base(message)
        {
            ConfigurationKey = configurationKey;
        }

        public ConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ConfigurationException(string message, string configurationKey, Exception innerException) 
            : base(message, innerException)
        {
            ConfigurationKey = configurationKey;
        }
    }
}