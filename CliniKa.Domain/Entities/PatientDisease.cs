using System;

namespace CliniKa.Domain.Entities
{
    public class PatientDisease
    {
        public int PatientId { get; set; }
        public int DiseaseId { get; set; }
        public DateTime DiagnosedDate { get; set; } = DateTime.UtcNow;
        public int? SourceSessionId { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Disease Disease { get; set; } = null!;
        public virtual ClinicalSession? SourceSession { get; set; }
    }
}