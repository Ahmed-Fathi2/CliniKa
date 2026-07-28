using System;

namespace CliniKa.Domain.Entities
{
    public class PatientAllergy
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? AllergyId { get; set; }
        public string? RawAllergenName { get; set; }
        public string? Severity { get; set; }
        public int? SourceSessionId { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Allergy? Allergy { get; set; }
        public virtual ClinicalSession? SourceSession { get; set; }
    }
}
