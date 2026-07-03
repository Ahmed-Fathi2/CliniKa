using System;

namespace CliniKa.Domain.Entities
{
    public class SessionAiReport
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string? PatientSummary { get; set; }
        public string? PossibleDiagnoses { get; set; }
        public string? DrugInteractions { get; set; }
        public string? Contraindications { get; set; }
        public string? SuggestedInvestigations { get; set; }
        public string? ClinicalAlerts { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ClinicalSession ClinicalSession { get; set; } = null!;
    }
}
