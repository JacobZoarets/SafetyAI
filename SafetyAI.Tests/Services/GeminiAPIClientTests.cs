using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Services.Implementation;
using SafetyAI.Services.Exceptions;

namespace SafetyAI.Tests.Services
{
    [TestClass]
    public class GeminiAPIClientTests
    {
        private GeminiAPIClient _geminiClient;

        [TestInitialize]
        public void Setup()
        {
            // Note: These tests require a valid Gemini API key in configuration
            // For unit testing, consider using a mock HTTP client
            try
            {
                _geminiClient = new GeminiAPIClient();
            }
            catch (InvalidOperationException)
            {
                // Skip tests if API key is not configured
                Assert.Inconclusive("Gemini API key not configured. Skipping integration tests.");
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _geminiClient?.Dispose();
        }

        [TestMethod]
        public async Task ProcessDocumentAsync_WithValidPDF_ShouldReturnAnalysisResult()
        {
            // Arrange
            var samplePdfData = CreateSamplePdfData();
            var contentType = "application/pdf";

            // Act
            var result = await _geminiClient.ProcessDocumentAsync(samplePdfData, contentType);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.ExtractedText);
            Assert.IsTrue(result.ConfidenceScore > 0);
            Assert.IsTrue(result.DetectedLanguages.Count > 0);
        }

        [TestMethod]
        public async Task ProcessDocumentAsync_WithValidImage_ShouldReturnAnalysisResult()
        {
            // Arrange
            var sampleImageData = CreateSampleImageData();
            var contentType = "image/jpeg";

            // Act
            var result = await _geminiClient.ProcessDocumentAsync(sampleImageData, contentType);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.ExtractedText);
            Assert.IsTrue(result.ConfidenceScore > 0);
        }

        [TestMethod]
        public async Task AnalyzeSafetyContentAsync_WithValidText_ShouldReturnAnalysis()
        {
            // Arrange
            var safetyText = "Employee slipped on wet floor in warehouse area. Minor injury to ankle. First aid administered on site. Recommend installing wet floor warning signs.";

            // Act
            var result = await _geminiClient.AnalyzeSafetyContentAsync(safetyText);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Summary);
            Assert.IsTrue(result.RiskScore >= 1 && result.RiskScore <= 10);
            Assert.IsTrue(result.AnalysisConfidence > 0);
        }

        [TestMethod]
        public async Task ProcessChatQueryAsync_WithSafetyQuestion_ShouldReturnResponse()
        {
            // Arrange
            var query = "What are the proper procedures for handling a chemical spill?";
            var sessionId = Guid.NewGuid().ToString();

            // Act
            var result = await _geminiClient.ProcessChatQueryAsync(query, sessionId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Response);
            Assert.AreEqual(sessionId, result.SessionId);
            Assert.IsTrue(result.ConfidenceScore > 0);
            Assert.IsTrue(result.Response.Length > 0);
        }

        [TestMethod]
        public async Task ProcessAudioAsync_WithValidAudio_ShouldReturnTranscription()
        {
            // Arrange
            var sampleAudioData = CreateSampleAudioData();
            var contentType = "audio/wav";

            // Act
            var result = await _geminiClient.ProcessAudioAsync(sampleAudioData, contentType);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.TranscribedText);
            Assert.IsNotNull(result.DetectedLanguage);
            Assert.IsTrue(result.TranscriptionConfidence > 0);
        }

        [TestMethod]
        public async Task CallWithRetryAsync_WithTransientFailure_ShouldRetryAndSucceed()
        {
            // Arrange
            int attemptCount = 0;
            Func<Task<string>> operation = async () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new System.Net.Http.HttpRequestException("Transient error");
                }
                return "Success";
            };

            // Act
            var result = await _geminiClient.CallWithRetryAsync(operation);

            // Assert
            Assert.AreEqual("Success", result);
            Assert.AreEqual(3, attemptCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ProcessDocumentAsync_WithInvalidData_ShouldThrowException()
        {
            // Arrange
            byte[] invalidData = null;
            var contentType = "application/pdf";

            // Act
            await _geminiClient.ProcessDocumentAsync(invalidData, contentType);

            // Assert - Exception expected
        }

        [TestMethod]
        public async Task AnalyzeSafetyContentAsync_WithEmptyText_ShouldReturnBasicAnalysis()
        {
            // Arrange
            var emptyText = "";

            // Act
            var result = await _geminiClient.AnalyzeSafetyContentAsync(emptyText);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Summary);
            Assert.IsTrue(result.AnalysisConfidence >= 0);
        }

        [TestMethod]
        public async Task ProcessChatQueryAsync_WithComplexQuery_ShouldProvideDetailedResponse()
        {
            // Arrange
            var complexQuery = "What are the OSHA requirements for fall protection in construction, and how do they differ from general industry standards?";
            var sessionId = Guid.NewGuid().ToString();

            // Act
            var result = await _geminiClient.ProcessChatQueryAsync(complexQuery, sessionId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Response);
            Assert.IsTrue(result.Response.Length > 100); // Expect detailed response
            Assert.IsTrue(result.SuggestedActions.Count >= 0);
        }

        // Helper methods for creating test data
        private byte[] CreateSamplePdfData()
        {
            // Create a minimal PDF-like structure for testing
            // In real tests, you would use actual PDF data
            var pdfHeader = "%PDF-1.4\n";
            var pdfContent = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
            var pdfTrailer = "trailer\n<< /Size 3 /Root 1 0 R >>\n%%EOF";
            
            return Encoding.UTF8.GetBytes(pdfHeader + pdfContent + pdfTrailer);
        }

        private byte[] CreateSampleImageData()
        {
            // Create a minimal JPEG header for testing
            // In real tests, you would use actual image data
            var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            var jpegData = new byte[1024];
            var jpegFooter = new byte[] { 0xFF, 0xD9 };
            
            var result = new byte[jpegHeader.Length + jpegData.Length + jpegFooter.Length];
            Array.Copy(jpegHeader, 0, result, 0, jpegHeader.Length);
            Array.Copy(jpegData, 0, result, jpegHeader.Length, jpegData.Length);
            Array.Copy(jpegFooter, 0, result, jpegHeader.Length + jpegData.Length, jpegFooter.Length);
            
            return result;
        }

        private byte[] CreateSampleAudioData()
        {
            // Create a minimal WAV header for testing
            // In real tests, you would use actual audio data
            var wavHeader = Encoding.ASCII.GetBytes("RIFF");
            var wavData = new byte[1024];
            var wavFormat = Encoding.ASCII.GetBytes("WAVE");
            
            var result = new byte[wavHeader.Length + 4 + wavFormat.Length + wavData.Length];
            Array.Copy(wavHeader, 0, result, 0, wavHeader.Length);
            // Add size (placeholder)
            result[4] = 0x24; result[5] = 0x08; result[6] = 0x00; result[7] = 0x00;
            Array.Copy(wavFormat, 0, result, 8, wavFormat.Length);
            Array.Copy(wavData, 0, result, 12, wavData.Length);
            
            return result;
        }
    }
}