using System;

namespace CliniKa.Application.DTOs.Patient
{
    public class SessionAiReportDto
    {
        public int Id { get; set; }
        public string? PatientSummary { get; set; }
        public string? PossibleDiagnoses { get; set; }
        public string? DrugInteractions { get; set; }
        public string? Contraindications { get; set; }
        public string? SuggestedInvestigations { get; set; }
        public string? ClinicalAlerts { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
