using System;

namespace CliniKa.Application.DTOs.Sessions
{
    public class SessionDiagnosisResultDto
    {
        public int SessionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AudioTranscript { get; set; }
        public string? ExtractedSymptoms { get; set; }
        public string? PatientSummary { get; set; }
        public string? PossibleDiagnoses { get; set; }
        public string? DrugInteractions { get; set; }
        public string? Contraindications { get; set; }
        public string? SuggestedInvestigations { get; set; }
        public string? ClinicalAlerts { get; set; }
        public string? FinalDiagnosis { get; set; }
        public DateTime? GeneratedAt { get; set; }
    }
}
