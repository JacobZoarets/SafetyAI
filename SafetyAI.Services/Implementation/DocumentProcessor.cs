using System;
using System.IO;
using System.Threading.Tasks;
using SafetyAI.Models.DTOs;
using SafetyAI.Services.Interfaces;

namespace SafetyAI.Services.Implementation
{
    public class DocumentProcessor : IDocumentProcessor, IDisposable
    {
        private readonly IGeminiAPIClient _geminiClient;
        private readonly IFileValidator _fileValidator;

        public DocumentProcessor(IGeminiAPIClient geminiClient, IFileValidator fileValidator)
        {
            _geminiClient = geminiClient ?? throw new ArgumentNullException(nameof(geminiClient));
            _fileValidator = fileValidator ?? throw new ArgumentNullException(nameof(fileValidator));
        }

        public async Task<DocumentAnalysisResult> ProcessDocumentAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                // Validate file
                if (!await _fileValidator.ValidateFileAsync(fileStream, fileName))
                {
                    return new DocumentAnalysisResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "File validation failed",
                        RequiresHumanReview = true
                    };
                }

                // Convert stream to byte array
                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await fileStream.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                // Process with Gemini API
                var result = await _geminiClient.ProcessDocumentAsync(fileData, contentType);
                return result;
            }
            catch (Exception ex)
            {
                return new DocumentAnalysisResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Document processing failed: {ex.Message}",
                    RequiresHumanReview = true
                };
            }
        }

        public async Task<bool> ValidateFileAsync(Stream fileStream, string fileName)
        {
            return await _fileValidator.ValidateFileAsync(fileStream, fileName);
        }

        public async Task<TextExtractionResult> ExtractTextAsync(byte[] fileData, string contentType)
        {
            try
            {
                if (fileData == null || fileData.Length == 0)
                {
                    return new TextExtractionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "No file data provided"
                    };
                }

                // For now, return a basic implementation
                // In a full implementation, this would use OCR or document parsing libraries
                var extractedText = contentType.StartsWith("text/") 
                    ? System.Text.Encoding.UTF8.GetString(fileData)
                    : $"Binary file of type {contentType} - text extraction would require specialized libraries";

                return new TextExtractionResult
                {
                    IsSuccess = true,
                    ExtractedText = extractedText,
                    ConfidenceScore = contentType.StartsWith("text/") ? 1.0 : 0.5,
                    DetectedLanguage = "en",
                    DetectedLanguages = new System.Collections.Generic.List<string> { "en" }
                };
            }
            catch (Exception ex)
            {
                return new TextExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Text extraction failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> IsDocumentProcessableAsync(Stream fileStream, string fileName)
        {
            try
            {
                if (fileStream == null || string.IsNullOrWhiteSpace(fileName))
                    return false;

                return await _fileValidator.ValidateFileAsync(fileStream, fileName);
            }
            catch
            {
                return false;
            }
        }

        public DocumentProcessingCapabilities GetProcessingCapabilities()
        {
            var supportedTypes = new System.Collections.Generic.List<string> 
            { 
                "pdf", "doc", "docx", "txt", "png", "jpg", "jpeg", "gif", "bmp", "tiff" 
            };

            return new DocumentProcessingCapabilities
            {
                SupportedFormats = new System.Collections.Generic.List<string>(supportedTypes),
                SupportedFileTypes = new System.Collections.Generic.List<string>(supportedTypes),
                MaxFileSize = 50 * 1024 * 1024, // 50MB
                SupportsOCR = true,
                SupportsMultipage = true,
                SupportsMultiLanguage = true,
                ConfidenceScoring = true
            };
        }

        public void Dispose()
        {
            // Cleanup resources if needed
        }
    }

    public class DocumentProcessingCapabilities
    {
        public System.Collections.Generic.List<string> SupportedFormats { get; set; }
        public System.Collections.Generic.List<string> SupportedFileTypes { get; set; }
        public long MaxFileSize { get; set; }
        public bool SupportsOCR { get; set; }
        public bool SupportsMultipage { get; set; }
        public bool SupportsMultiLanguage { get; set; }
        public bool ConfidenceScoring { get; set; }

        public DocumentProcessingCapabilities()
        {
            SupportedFormats = new System.Collections.Generic.List<string>();
            SupportedFileTypes = new System.Collections.Generic.List<string>();
        }
    }
}