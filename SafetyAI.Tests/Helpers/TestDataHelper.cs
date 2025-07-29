using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SafetyAI.Models.Entities;

namespace SafetyAI.Tests.Helpers
{
    public static class TestDataHelper
    {
        public static byte[] CreateSamplePDFBytes()
        {
            // Create a simple PDF-like byte array for testing
            var pdfHeader = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
            var content = System.Text.Encoding.UTF8.GetBytes("Sample safety incident report content for testing");
            var result = new byte[pdfHeader.Length + content.Length];
            Array.Copy(pdfHeader, 0, result, 0, pdfHeader.Length);
            Array.Copy(content, 0, result, pdfHeader.Length, content.Length);
            return result;
        }

        public static byte[] CreateSampleImageBytes()
        {
            // Create a simple JPEG-like byte array for testing
            var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
            var content = System.Text.Encoding.UTF8.GetBytes("Sample image content for testing");
            var result = new byte[jpegHeader.Length + content.Length];
            Array.Copy(jpegHeader, 0, result, 0, jpegHeader.Length);
            Array.Copy(content, 0, result, jpegHeader.Length, content.Length);
            return result;
        }

        public static byte[] CreateLargePDFBytes(int sizeInBytes)
        {
            var pdfHeader = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
            var content = new byte[sizeInBytes - pdfHeader.Length];
            var random = new Random();
            random.NextBytes(content);
            
            var result = new byte[sizeInBytes];
            Array.Copy(pdfHeader, 0, result, 0, pdfHeader.Length);
            Array.Copy(content, 0, result, pdfHeader.Length, content.Length);
            return result;
        }

        public static async Task SeedTestReports(TestDbContext context, int count)
        {
            var reports = new List<SafetyReport>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                var report = new SafetyReport
                {
                    Id = Guid.NewGuid(),
                    FileName = $"test-report-{i}.pdf",
                    FileSize = random.Next(1000, 10000),
                    ContentType = "application/pdf",
                    ExtractedText = $"Test incident report {i} content",
                    Status = "Completed",
                    UploadedBy = $"test-user-{i % 10}",
                    UploadedDate = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                    ProcessedDate = DateTime.UtcNow.AddDays(-random.Next(0, 365)),
                    IsActive = true
                };
                reports.Add(report);
            }

            context.SafetyReports.AddRange(reports);
            await context.SaveChangesAsync();
        }
    }
}