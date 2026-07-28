using System;

namespace CliniKa.Application.DTOs.Patient
{
    public class UploadedDocumentDto
    {
        public int Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? ExtractedText { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
