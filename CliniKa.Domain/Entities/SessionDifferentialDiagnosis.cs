using System;

namespace CliniKa.Domain.Entities
{
    public class SessionDifferentialDiagnosis
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public int? Rank { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ClinicalSession ClinicalSession { get; set; } = null!;
    }
}
