using System.Configuration;

namespace SafetyAI.Services.Configuration
{
    public static class ServiceConfiguration
    {
        public static string GeminiAPIKey => ConfigurationManager.AppSettings["GeminiAPIKey"];
        public static string GeminiAPIEndpoint => ConfigurationManager.AppSettings["GeminiAPIEndpoint"] ?? "https://generativelanguage.googleapis.com/v1beta";
        public static int MaxFileSize => int.Parse(ConfigurationManager.AppSettings["MaxFileSize"] ?? "10485760"); // 10MB
        public static int ProcessingTimeoutSeconds => int.Parse(ConfigurationManager.AppSettings["ProcessingTimeoutSeconds"] ?? "30");
        public static int RetryCount => int.Parse(ConfigurationManager.AppSettings["RetryCount"] ?? "3");
        public static int RetryDelaySeconds => int.Parse(ConfigurationManager.AppSettings["RetryDelaySeconds"] ?? "2");
        
        public static readonly string[] SupportedFileTypes = { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
        public static readonly string[] SupportedAudioTypes = { ".wav", ".mp3", ".m4a", ".ogg" };
        
        public static readonly string[] SupportedLanguages = { "en", "he", "ar", "ru" };
    }
}