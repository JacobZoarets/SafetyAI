using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyAI.Services.Implementation;
using SafetyAI.Services.Exceptions;

namespace SafetyAI.Tests.Services
{
    [TestClass]
    public class FileValidatorTests
    {
        private FileValidator _fileValidator;

        [TestInitialize]
        public void Setup()
        {
            _fileValidator = new FileValidator();
        }

        [TestMethod]
        public async Task ValidateFileAsync_WithValidPDF_ShouldReturnTrue()
        {
            // Arrange
            var pdfData = CreateValidPdfData();
            using (var stream = new MemoryStream(pdfData))
            {
                var fileName = "test_document.pdf";

                // Act
                var result = await _fileValidator.ValidateFileAsync(stream, fileName);

                // Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public async Task ValidateFileAsync_WithValidJPEG_ShouldReturnTrue()
        {
            // Arrange
            var jpegData = CreateValidJpegData();
            using (var stream = new MemoryStream(jpegData))
            {
                var fileName = "test_image.jpg";

                // Act
                var result = await _fileValidator.ValidateFileAsync(stream, fileName);

                // Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public async Task ValidateFileAsync_WithValidPNG_ShouldReturnTrue()
        {
            // Arrange
            var pngData = CreateValidPngData();
            using (var stream = new MemoryStream(pngData))
            {
                var fileName = "test_image.png";

                // Act
                var result = await _fileValidator.ValidateFileAsync(stream, fileName);

                // Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public async Task ValidateFileAsync_WithValidWAV_ShouldReturnTrue()
        {
            // Arrange
            var wavData = CreateValidWavData();
            using (var stream = new MemoryStream(wavData))
            {
                var fileName = "test_audio.wav";

                // Act
                var result = await _fileValidator.ValidateFileAsync(stream, fileName);

                // Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileValidationException))]
        public async Task ValidateFileAsync_WithUnsupportedFileType_ShouldThrowException()
        {
            // Arrange
            var textData = Encoding.UTF8.GetBytes("This is a text file");
            using (var stream = new MemoryStream(textData))
            {
                var fileName = "document.txt";

                // Act
                await _fileValidator.ValidateFileAsync(stream, fileName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileValidationException))]
        public async Task ValidateFileAsync_WithEmptyFileName_ShouldThrowException()
        {
            // Arrange
            var pdfData = CreateValidPdfData();
            using (var stream = new MemoryStream(pdfData))
            {
                var fileName = "";

                // Act
                await _fileValidator.ValidateFileAsync(stream, fileName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileValidationException))]
        public async Task ValidateFileAsync_WithNullStream_ShouldThrowException()
        {
            // Arrange
            Stream stream = null;
            var fileName = "test.pdf";

            // Act
            await _fileValidator.ValidateFileAsync(stream, fileName);
        }

        [TestMethod]
        [ExpectedException(typeof(FileValidationException))]
        public async Task ValidateFileAsync_WithOversizedFile_ShouldThrowException()
        {
            // Arrange
            var largeData = new byte[20 * 1024 * 1024]; // 20MB file (exceeds 10MB limit)
            using (var stream = new MemoryStream(largeData))
            {
                var fileName = "large_file.pdf";

                // Act
                await _fileValidator.ValidateFileAsync(stream, fileName);
            }
        }

        [TestMethod]
        public void IsValidFileType_WithSupportedExtension_ShouldReturnTrue()
        {
            // Arrange
            var fileName = "document.pdf";

            // Act
            var result = _fileValidator.IsValidFileType(fileName);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsValidFileType_WithUnsupportedExtension_ShouldReturnFalse()
        {
            // Arrange
            var fileName = "document.docx";

            // Act
            var result = _fileValidator.IsValidFileType(fileName);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValidFileType_WithEmptyFileName_ShouldReturnFalse()
        {
            // Arrange
            var fileName = "";

            // Act
            var result = _fileValidator.IsValidFileType(fileName);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValidFileSize_WithValidSize_ShouldReturnTrue()
        {
            // Arrange
            var fileSize = 5 * 1024 * 1024; // 5MB

            // Act
            var result = _fileValidator.IsValidFileSize(fileSize);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsValidFileSize_WithOversizedFile_ShouldReturnFalse()
        {
            // Arrange
            var fileSize = 15 * 1024 * 1024; // 15MB (exceeds 10MB limit)

            // Act
            var result = _fileValidator.IsValidFileSize(fileSize);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValidFileSize_WithZeroSize_ShouldReturnFalse()
        {
            // Arrange
            var fileSize = 0;

            // Act
            var result = _fileValidator.IsValidFileSize(fileSize);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task IsValidFileContentAsync_WithValidPDFContent_ShouldReturnTrue()
        {
            // Arrange
            var pdfData = CreateValidPdfData();
            using (var stream = new MemoryStream(pdfData))
            {
                var contentType = "application/pdf";

                // Act
                var result = await _fileValidator.IsValidFileContentAsync(stream, contentType);

                // Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public async Task IsValidFileContentAsync_WithInvalidPDFContent_ShouldReturnFalse()
        {
            // Arrange
            var invalidData = Encoding.UTF8.GetBytes("This is not a PDF");
            using (var stream = new MemoryStream(invalidData))
            {
                var contentType = "application/pdf";

                // Act
                var result = await _fileValidator.IsValidFileContentAsync(stream, contentType);

                // Assert
                Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public async Task IsValidFileContentAsync_WithValidJPEGContent_ShouldReturnTrue()
        {
            // Arrange
            var jpegData = CreateValidJpegData();
            using (var stream = new MemoryStream(jpegData))
            {
                var contentType = "image/jpeg";

                // Act
                var result = await _fileValidator.IsValidFileContentAsync(stream, contentType);

                // Assert
                Assert.IsTrue(result);
            }
        }

        [TestMethod]
        public async Task ValidateFileDetailed_WithValidFile_ShouldReturnSuccessResult()
        {
            // Arrange
            var pdfData = CreateValidPdfData();
            using (var stream = new MemoryStream(pdfData))
            {
                var fileName = "test.pdf";

                // Act
                var result = await _fileValidator.ValidateFileDetailed(stream, fileName);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.IsValid);
                Assert.IsNull(result.ErrorMessage);
            }
        }

        [TestMethod]
        public async Task ValidateFileDetailed_WithInvalidFile_ShouldReturnFailureResult()
        {
            // Arrange
            var textData = Encoding.UTF8.GetBytes("Not a valid file");
            using (var stream = new MemoryStream(textData))
            {
                var fileName = "invalid.txt";

                // Act
                var result = await _fileValidator.ValidateFileDetailed(stream, fileName);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsFalse(result.IsValid);
                Assert.IsNotNull(result.ErrorMessage);
                Assert.IsNotNull(result.SuggestedAction);
            }
        }

        // Helper methods for creating valid test data
        private byte[] CreateValidPdfData()
        {
            var pdfHeader = "%PDF-1.4\n";
            var pdfContent = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
            var pdfTrailer = "trailer\n<< /Size 3 /Root 1 0 R >>\n%%EOF";
            
            return Encoding.UTF8.GetBytes(pdfHeader + pdfContent + pdfTrailer);
        }

        private byte[] CreateValidJpegData()
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

        private byte[] CreateValidPngData()
        {
            var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var pngData = new byte[1024];
            
            var result = new byte[pngSignature.Length + pngData.Length];
            Array.Copy(pngSignature, 0, result, 0, pngSignature.Length);
            Array.Copy(pngData, 0, result, pngSignature.Length, pngData.Length);
            
            return result;
        }

        private byte[] CreateValidWavData()
        {
            var riffHeader = Encoding.ASCII.GetBytes("RIFF");
            var fileSize = BitConverter.GetBytes(36); // Placeholder size
            var waveHeader = Encoding.ASCII.GetBytes("WAVE");
            var fmtChunk = Encoding.ASCII.GetBytes("fmt ");
            var fmtSize = BitConverter.GetBytes(16);
            var audioFormat = BitConverter.GetBytes((short)1);
            var numChannels = BitConverter.GetBytes((short)1);
            var sampleRate = BitConverter.GetBytes(44100);
            var byteRate = BitConverter.GetBytes(88200);
            var blockAlign = BitConverter.GetBytes((short)2);
            var bitsPerSample = BitConverter.GetBytes((short)16);
            var dataChunk = Encoding.ASCII.GetBytes("data");
            var dataSize = BitConverter.GetBytes(0);
            
            var result = new byte[44]; // Standard WAV header size
            var offset = 0;
            
            Array.Copy(riffHeader, 0, result, offset, riffHeader.Length); offset += riffHeader.Length;
            Array.Copy(fileSize, 0, result, offset, fileSize.Length); offset += fileSize.Length;
            Array.Copy(waveHeader, 0, result, offset, waveHeader.Length); offset += waveHeader.Length;
            Array.Copy(fmtChunk, 0, result, offset, fmtChunk.Length); offset += fmtChunk.Length;
            Array.Copy(fmtSize, 0, result, offset, fmtSize.Length); offset += fmtSize.Length;
            Array.Copy(audioFormat, 0, result, offset, audioFormat.Length); offset += audioFormat.Length;
            Array.Copy(numChannels, 0, result, offset, numChannels.Length); offset += numChannels.Length;
            Array.Copy(sampleRate, 0, result, offset, sampleRate.Length); offset += sampleRate.Length;
            Array.Copy(byteRate, 0, result, offset, byteRate.Length); offset += byteRate.Length;
            Array.Copy(blockAlign, 0, result, offset, blockAlign.Length); offset += blockAlign.Length;
            Array.Copy(bitsPerSample, 0, result, offset, bitsPerSample.Length); offset += bitsPerSample.Length;
            Array.Copy(dataChunk, 0, result, offset, dataChunk.Length); offset += dataChunk.Length;
            Array.Copy(dataSize, 0, result, offset, dataSize.Length);
            
            return result;
        }
    }
}