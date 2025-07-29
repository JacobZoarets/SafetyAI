using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Services.Implementation;
using SafetyAI.Services.Interfaces;
using SafetyAI.Services.Exceptions;
using SafetyAI.Models.DTOs;

namespace SafetyAI.Tests.Services
{
    [TestClass]
    public class DocumentProcessorTests
    {
        private DocumentProcessor _documentProcessor;
        private MockGeminiAPIClient _mockGeminiClient;
        private FileValidator _fileValidator;

        [TestInitialize]
        public void Setup()
        {
            _mockGeminiClient = new MockGeminiAPIClient();
            _fileValidator = new FileValidator();
            _documentProcessor = new DocumentProcessor(_mockGeminiClient, _fileValidator);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _documentProcessor?.Dispose();
        }

        [TestMethod]
        public async Task ProcessDocumentAsync_WithValidPDF_ShouldReturnSuccessResult()
        {
            // Arrange
            var pdfData = CreateSamplePdfData();
            using (var stream = new MemoryStream(pdfData))
            {
                var fileName = "test_report.pdf";
                var contentType = "application/pdf";

                _mockGeminiClient.SetupSuccessResponse("Sample extracted text from PDF document", 0.95);

                // Act
                var result = await _documentProcessor.ProcessDocumentAsync(stream, fileName, contentType);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual("Sample extracted text from PDF document", result.ExtractedText);
                Assert.AreEqual(0.95, result.ConfidenceScore, 0.01);
                Assert.IsFalse(result.RequiresHumanReview);
            }
        }

        [TestMethod]
        public async Task ProcessDocumentAsync_WithValidImage_ShouldReturnSuccessResult()
        {
            // Arrange
            var imageData = CreateSampleJpegData();
            using (var stream = new MemoryStream(imageData))
            {
                var fileName = "incident_photo.jpg";
                var contentType = "image/jpeg";

                _mockGeminiClient.SetupSuccessResponse("Text extracted from image", 0.88);

                // Act
                var result = await _documentProcessor.ProcessDocumentAsync(stream, fileName, contentType);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.IsSuccess);
                Assert.AreEqual("Text extracted from image", result.ExtractedText);
                Assert.AreEqual(0.88, result.ConfidenceScore, 0.01);
            }
        }

        [TestMethod]
        public async Task ProcessDocumentAsync_WithLowConfidence_ShouldRequireHumanReview()
        {
            // Arrange
            var pdfData = CreateSamplePdfData();
            using (var stream = new MemoryStream(pdfData))
            {
                var fileName = "low_quality.pdf";
                var contentType = "application/pdf";

                _mockGeminiClient.SetupSuccessResponse("Partially extracted text", 0.65);

                // Act
                var result = await _documentProcessor.ProcessDocumentAsync(stream, fileName, contentType);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.IsSuccess);
                Assert.IsTrue(result.RequiresHumanReview);
                Assert.AreEqual(0.65, result.ConfidenceScore, 0.01);
            }
        }

        [TestMethod]
        public async Task ProcessDocumentAsync_WithInvalidFile_ShouldReturnFailureResult()
        {
            // Arrange
            var invalidData = Encoding.UTF8.GetBytes("This is not a valid PDF");
            using (var stream = new MemoryStream(invalidData))
            {
                var fileName = "invalid.pdf";
                var contentType = "application/pdf";

                // Act
                var result = await _documentProcessor.ProcessDocumentAsync(stream, fileName, contentType);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsFalse(result.IsSuccess);
                Assert.IsTrue(result.RequiresHumanReview);
                Assert.IsNotNull(result.ErrorMessage);
            }
        }

        [TestMethod]
        public async Task ProcessDocumentAsync_WithOversizedFile_ShouldReturnFailureResult()
        {
            // Arrange
            var largeData = new byte[20 * 1024 * 1024]; // 20MB file
            using (var stream = new MemoryStream(largeData))
            {
                var fileName = "large_file.pdf";
                var contentType = "application/pdf";

                // Act
                var result = await _documentProcessor.ProcessDocumentAsync(stream, fileName, contentType);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsFalse(result.IsSuccess);
                Assert.IsTrue(result.RequiresHumanReview);
                Assert.IsTrue(result.ErrorMessage.Contains("size"));
            }
        }

        [TestMethod]
        public async Task ValidateFileAsync_WithValidPDF_ShouldReturnTrue()
        {
            // Arrange
            var pdfData = CreateSamplePdfData();
            using (var stream = new MemoryStream(pdfData))
            {
                var fileName = "valid.pdf";

                // Act
                var result = await _documentProcessor.ValidateFileAsync(stream, fileName);

                // Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public async Task ValidateFileAsync_WithInvalidExtension_ShouldReturnFalse()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Some text content");
            using (var stream = new MemoryStream(data))
            {
                var fileName = "document.txt";

                // Act
                var result = await _documentProcessor.ValidateFileAsync(stream, fileName);

                // Assert
                Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public async Task ExtractTextAsync_WithValidData_ShouldReturnExtractionResult()
        {
            // Arrange
            var pdfData = CreateSamplePdfData();
            var contentType = "application/pdf";

            _mockGeminiClient.SetupSuccessResponse("Extracted text content", 0.92);

            // Act
            var result = await _documentProcessor.ExtractTextAsync(pdfData, contentType);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("Extracted text content", result.ExtractedText);
            Assert.AreEqual(0.92, result.ConfidenceScore, 0.01);
        }

        [TestMethod]
        public async Task ExtractTextAsync_WithGeminiFailure_ShouldReturnFailureResult()
        {
            // Arrange
            var pdfData = CreateSamplePdfData();
            var contentType = "application/pdf";

            _mockGeminiClient.SetupFailureResponse("API error occurred");

            // Act
            var result = await _documentProcessor.ExtractTextAsync(pdfData, contentType);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.AreEqual(0.0, result.ConfidenceScore);
        }

        [TestMethod]
        public async Task IsDocumentProcessableAsync_WithSupportedFile_ShouldReturnTrue()
        {
            // Arrange
            var pdfData = CreateSamplePdfData();
            using (var stream = new MemoryStream(pdfData))
            {
                var fileName = "document.pdf";

                // Act
                var result = await _documentProcessor.IsDocumentProcessableAsync(stream, fileName);

                // Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public async Task IsDocumentProcessableAsync_WithUnsupportedFile_ShouldReturnFalse()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Some content");
            using (var stream = new MemoryStream(data))
            {
                var fileName = "document.docx";

                // Act
                var result = await _documentProcessor.IsDocumentProcessableAsync(stream, fileName);

                // Assert
                Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public void GetProcessingCapabilities_ShouldReturnValidCapabilities()
        {
            // Act
            var capabilities = _documentProcessor.GetProcessingCapabilities();

            // Assert
            Assert.IsNotNull(capabilities);
            Assert.IsNotNull(capabilities.SupportedFileTypes);
            Assert.IsTrue(capabilities.SupportedFileTypes.Count > 0);
            Assert.IsTrue(capabilities.MaxFileSize > 0);
            Assert.IsTrue(capabilities.SupportsMultiLanguage);
            Assert.IsTrue(capabilities.ConfidenceScoring);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ProcessDocumentAsync_WithNullStream_ShouldThrowException()
        {
            // Act
            await _documentProcessor.ProcessDocumentAsync(null, "test.pdf", "application/pdf");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ProcessDocumentAsync_WithEmptyFileName_ShouldThrowException()
        {
            // Arrange
            using (var stream = new MemoryStream(new byte[100]))
            {
                // Act
                await _documentProcessor.ProcessDocumentAsync(stream, "", "application/pdf");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ProcessDocumentAsync_WithEmptyContentType_ShouldThrowException()
        {
            // Arrange
            using (var stream = new MemoryStream(new byte[100]))
            {
                // Act
                await _documentProcessor.ProcessDocumentAsync(stream, "test.pdf", "");
            }
        }

        // Helper methods for creating test data
        private byte[] CreateSamplePdfData()
        {
            var pdfHeader = "%PDF-1.4\n";
            var pdfContent = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
            var pdfTrailer = "trailer\n<< /Size 3 /Root 1 0 R >>\n%%EOF";
            
            return Encoding.UTF8.GetBytes(pdfHeader + pdfContent + pdfTrailer);
        }

        private byte[] CreateSampleJpegData()
        {
            var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            var jpegData = new byte[1024];
            var jpegFooter = new byte[] { 0xFF, 0xD9 };
            
            var result = new byte[jpegHeader.Length + jpegData.Length + jpegFooter.Length];
            Array.Copy(jpegHeader, 0, result, 0, jpegHeader.Length);
            Array.Copy(jpegData, 0, result, jpegHeader.Length, jpegData.Length);
            Array.Copy(jpegFooter, 0, result, jpegHeader.Length + jpegData.Length, jpegFooter.Length);
            
            return result;
        }
    }

    // Mock implementation for testing
    public class MockGeminiAPIClient : IGeminiAPIClient
    {
        private DocumentAnalysisResult _mockResponse;
        private bool _shouldFail;
        private string _failureMessage;

        public void SetupSuccessResponse(string extractedText, double confidence)
        {
            _shouldFail = false;
            _mockResponse = new DocumentAnalysisResult
            {
                ExtractedText = extractedText,
                ConfidenceScore = confidence,
                DetectedLanguages = new System.Collections.Generic.List<string> { "en" },
                ProcessingTime = TimeSpan.FromMilliseconds(1500),
                RequiresHumanReview = confidence < 0.8,
                IsSuccess = true
            };
        }

        public void SetupFailureResponse(string errorMessage)
        {
            _shouldFail = true;
            _failureMessage = errorMessage;
            _mockResponse = new DocumentAnalysisResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                RequiresHumanReview = true
            };
        }

        public async Task<DocumentAnalysisResult> ProcessDocumentAsync(byte[] fileData, string contentType)
        {
            await Task.Delay(100); // Simulate processing time
            
            if (_shouldFail)
            {
                throw new GeminiAPIException(_failureMessage);
            }
            
            return _mockResponse;
        }

        public async Task<SafetyAnalysisResult> AnalyzeSafetyContentAsync(string text)
        {
            await Task.Delay(100);
            return new SafetyAnalysisResult
            {
                Summary = "Mock analysis result",
                AnalysisConfidence = 0.9
            };
        }

        public async Task<ChatResponse> ProcessChatQueryAsync(string query, string sessionId)
        {
            await Task.Delay(100);
            return new ChatResponse
            {
                Response = "Mock chat response",
                SessionId = sessionId,
                ConfidenceScore = 0.9
            };
        }

        public async Task<AudioProcessingResult> ProcessAudioAsync(byte[] audioData, string contentType)
        {
            await Task.Delay(100);
            return new AudioProcessingResult
            {
                TranscribedText = "Mock transcription",
                IsSuccess = true,
                TranscriptionConfidence = 0.9
            };
        }

        public async Task<T> CallWithRetryAsync<T>(Func<Task<T>> apiCall)
        {
            return await apiCall();
        }

        public void Dispose()
        {
            // Mock disposal
        }
    }
}