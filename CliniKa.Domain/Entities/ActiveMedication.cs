using System;

namespace CliniKa.Domain.Entities
{
    public class ActiveMedication
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int? SourceSessionId { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual ClinicalSession? SourceSession { get; set; }
    }
}
