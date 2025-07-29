using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SafetyAI.Services.Interfaces;

namespace SafetyAI.Services.Implementation
{
    public class FileValidator : IFileValidator
    {
        private static readonly string[] AllowedExtensions = 
        {
            ".pdf", ".doc", ".docx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff"
        };

        private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

        public async Task<bool> ValidateFileAsync(Stream fileStream, string fileName)
        {
            try
            {
                if (fileStream == null || string.IsNullOrWhiteSpace(fileName))
                    return false;

                if (!IsValidFileType(fileName))
                    return false;

                if (!IsValidFileSize(fileStream.Length))
                    return false;

                var extension = Path.GetExtension(fileName).ToLower();
                return await IsValidFileContentAsync(fileStream, GetContentType(extension));
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidFileType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLower();
            return AllowedExtensions.Contains(extension);
        }

        public bool IsValidFileSize(long fileSize)
        {
            return fileSize > 0 && fileSize <= MaxFileSize;
        }

        public async Task<bool> IsValidFileContentAsync(Stream fileStream, string contentType)
        {
            try
            {
                if (fileStream == null || fileStream.Length == 0)
                    return false;

                // Basic validation - check if file has content
                var buffer = new byte[1024];
                var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                
                // Reset stream position
                if (fileStream.CanSeek)
                    fileStream.Position = 0;

                return bytesRead > 0;
            }
            catch
            {
                return false;
            }
        }

        private string GetContentType(string extension)
        {
            switch (extension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".doc":
                    return "application/msword";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".txt":
                    return "text/plain";
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".tiff":
                    return "image/tiff";
                default:
                    return "application/octet-stream";
            }
        }

        public async Task<FileValidationResult> ValidateFileDetailed(Stream fileStream, string fileName)
        {
            try
            {
                var result = new FileValidationResult();

                if (fileStream == null || string.IsNullOrWhiteSpace(fileName))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "File stream or filename is null/empty";
                    result.SuggestedAction = "Please provide a valid file";
                    return result;
                }

                if (!IsValidFileType(fileName))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"File type not supported: {Path.GetExtension(fileName)}";
                    result.SuggestedAction = $"Please use one of the supported formats: {string.Join(", ", AllowedExtensions)}";
                    return result;
                }

                if (!IsValidFileSize(fileStream.Length))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"File size {fileStream.Length} bytes exceeds maximum allowed size of {MaxFileSize} bytes";
                    result.SuggestedAction = "Please reduce file size or split into smaller files";
                    return result;
                }

                var extension = Path.GetExtension(fileName).ToLower();
                var contentValid = await IsValidFileContentAsync(fileStream, GetContentType(extension));
                
                if (!contentValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "File content validation failed";
                    result.SuggestedAction = "Please ensure the file is not corrupted";
                    return result;
                }

                result.IsValid = true;
                result.ErrorMessage = null;
                result.SuggestedAction = null;
                return result;
            }
            catch (Exception ex)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Validation error: {ex.Message}",
                    SuggestedAction = "Please contact support if the issue persists"
                };
            }
        }
    }
}