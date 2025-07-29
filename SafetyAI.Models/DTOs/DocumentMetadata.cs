using System;

namespace SafetyAI.Models.DTOs
{
    public class DocumentMetadata
    {
        public string FileName { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadDate { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
    }
}