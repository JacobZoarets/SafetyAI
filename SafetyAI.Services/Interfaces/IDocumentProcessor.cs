using System.IO;
using System.Threading.Tasks;
using SafetyAI.Models.DTOs;

namespace SafetyAI.Services.Interfaces
{
    public interface IDocumentProcessor
    {
        Task<DocumentAnalysisResult> ProcessDocumentAsync(Stream fileStream, string fileName, string contentType);
        Task<bool> ValidateFileAsync(Stream fileStream, string fileName);
        Task<TextExtractionResult> ExtractTextAsync(byte[] fileData, string contentType);
    }

    public class TextExtractionResult
    {
        public string ExtractedText { get; set; }
        public double ConfidenceScore { get; set; }
        public string DetectedLanguage { get; set; }
        public System.Collections.Generic.List<string> DetectedLanguages { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public TextExtractionResult()
        {
            DetectedLanguages = new System.Collections.Generic.List<string>();
        }
    }

    public class DocumentMetadata
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string UploadedBy { get; set; }
        public System.DateTime UploadedDate { get; set; }
    }
}