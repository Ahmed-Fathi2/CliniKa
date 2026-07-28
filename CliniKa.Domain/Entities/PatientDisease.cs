using System;

namespace CliniKa.Domain.Entities
{
    public class PatientDisease
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? DiseaseId { get; set; }
        public string? RawDiseaseName { get; set; }
        public DateTime DiagnosedDate { get; set; } = DateTime.UtcNow;
        public int? SourceSessionId { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Disease? Disease { get; set; }
        public virtual ClinicalSession? SourceSession { get; set; }
    }
}