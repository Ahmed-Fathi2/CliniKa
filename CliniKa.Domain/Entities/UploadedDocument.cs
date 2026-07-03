using System;

namespace CliniKa.Domain.Entities
{
    public class UploadedDocument
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? ExtractedText { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ClinicalSession ClinicalSession { get; set; } = null!;
    }
}
