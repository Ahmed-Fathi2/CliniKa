using System;

namespace CliniKa.Domain.Entities
{
    public class PrescribedMedication
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Notes { get; set; }

        // Navigation properties
        public virtual ClinicalSession ClinicalSession { get; set; } = null!;
    }
}
