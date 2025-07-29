using System.IO;
using System.Threading.Tasks;

namespace SafetyAI.Services.Interfaces
{
    public interface IFileValidator
    {
        Task<bool> ValidateFileAsync(Stream fileStream, string fileName);
        bool IsValidFileType(string fileName);
        bool IsValidFileSize(long fileSize);
        Task<bool> IsValidFileContentAsync(Stream fileStream, string contentType);
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public string SuggestedAction { get; set; }
    }
}